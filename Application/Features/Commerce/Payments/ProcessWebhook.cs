using Application.Common.Interfaces.Payments;

namespace Application.Features.Commerce.Webhooks;

public record ProcessPaymentWebhookCommand(string Payload, string SignatureHeader) : IRequest<WebhookProcessResult>;

public record WebhookProcessResult(bool Processed, string EventType, string? Reason = null);

public class ProcessPaymentWebhookCommandHandler(
    IWebhookEventRepository webhookRepo,
    IOrderRepository orderRepo,
    ISubscriptionRepository subscriptionRepo,
    IPaymentWebhookParser webhookParser,
    IUnitOfWork uow,
    ICacheService cacheService,
    ILogger<ProcessPaymentWebhookCommandHandler> logger)
    : IRequestHandler<ProcessPaymentWebhookCommand, WebhookProcessResult>
{
    private static string GetSubscriptionCacheKey(Guid tenantId) => $"tenant:{tenantId}:subscription:active";

    public async Task<WebhookProcessResult> Handle(ProcessPaymentWebhookCommand cmd, CancellationToken ct)
    {
        WebhookParseResult parsed;
        try
        {
            parsed = webhookParser.ParseEvent(cmd.Payload, cmd.SignatureHeader);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Assinatura de Webhook inválida detectada.");
            throw new ConflictException("Falha na validação criptográfica do Webhook.");
        }

        if (await webhookRepo.ExistsAsync(parsed.EventId, ct))
        {
            logger.LogInformation("Evento de Webhook {EventId} já foi processado anteriormente.", parsed.EventId);
            return new WebhookProcessResult(false, parsed.EventType, "Já processado.");
        }

        var webhookEvent = WebhookLogEvent.Create(parsed.EventId, parsed.EventType, parsed.RawPayload);
        await webhookRepo.AddAsync(webhookEvent, ct);
        await uow.CommitAsync(ct);

        try
        {
            await RouteEventAsync(parsed, ct);
            webhookEvent.MarkProcessed();
            await uow.CommitAsync(ct);

            logger.LogInformation("Evento {EventId} ({EventType}) processado com sucesso.", parsed.EventId, parsed.EventType);
            return new WebhookProcessResult(true, parsed.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha crítica ao processar regras de negócio do evento {EventId}.", parsed.EventId);
            webhookEvent.MarkFailed(ex.Message);
            await uow.CommitAsync(ct);
            throw;
        }
    }

    private async Task RouteEventAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        if (!Enum.TryParse<WebhookEventType>(parsed.EventType, true, out var eventType))
        {
            logger.LogDebug("Evento ignorado por não possuir mapeamento de negócio: {EventType}", parsed.EventType);
            return;
        }

        switch (eventType)
        {
            case WebhookEventType.PaymentSucceeded:
                await HandlePaymentSucceededAsync(parsed, ct);
                break;
            case WebhookEventType.PaymentFailed:
                await HandlePaymentFailedAsync(parsed, ct);
                break;
            case WebhookEventType.ChargeRefunded:
                await HandleChargeRefundedAsync(parsed, ct);
                break;
            case WebhookEventType.SubscriptionDeleted:
                await HandleSubscriptionDeletedAsync(parsed, ct);
                break;
            case WebhookEventType.InvoicePaymentFailed:
                await HandleInvoicePaymentFailedAsync(parsed, ct);
                break;
        }
    }

    private async Task HandlePaymentSucceededAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        var order = await orderRepo.GetByPaymentIntentIdAsync(parsed.PaymentIntentId, ct);
        if (order is null) return;

        if (order.Status != OrderStatus.Pending) return;

        var payment = order.Payments.FirstOrDefault(p => p.ExternalPaymentIntentId == parsed.PaymentIntentId);
        if (payment is null) return;

        payment.MarkSucceeded(parsed.ChargeId ?? string.Empty, "webhook:payment_intent.succeeded");
        order.ConfirmPayment();
    }

    private async Task HandlePaymentFailedAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        var order = await orderRepo.GetByPaymentIntentIdAsync(parsed.PaymentIntentId, ct);
        if (order is null) return;

        var payment = order.Payments.FirstOrDefault(p => p.ExternalPaymentIntentId == parsed.PaymentIntentId);
        if (payment is null) return;

        payment.MarkFailed(parsed.FailureMessage ?? "Pagamento recusado pela operadora.");
    }

    private async Task HandleChargeRefundedAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parsed.ChargeId)) return;

        var order = await orderRepo.GetByChargeIdAsync(parsed.ChargeId, ct);
        if (order is null) return;

        var payment = order.Payments.FirstOrDefault(p => p.ExternalChargeId == parsed.ChargeId);
        if (payment is null) return;

        payment.RegisterRefund(parsed.Amount ?? payment.Amount);
    }

    private async Task HandleSubscriptionDeletedAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parsed.SubscriptionId)) return;

        var subscription = await subscriptionRepo.GetByExternalIdAsync(parsed.SubscriptionId, ct);
        if (subscription is null || subscription.Status == SubscriptionStatus.Cancelled) return;

        subscription.Cancel();
        await cacheService.RemoveAsync(GetSubscriptionCacheKey(subscription.TenantId), ct);
    }

    private async Task HandleInvoicePaymentFailedAsync(WebhookParseResult parsed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parsed.SubscriptionId)) return;

        var subscription = await subscriptionRepo.GetByExternalIdAsync(parsed.SubscriptionId, ct);
        if (subscription is null || subscription.Status == SubscriptionStatus.Cancelled || subscription.Status == SubscriptionStatus.Expired) return;

        subscription.MarkPastDue();
        await cacheService.RemoveAsync(GetSubscriptionCacheKey(subscription.TenantId), ct);
    }
}

public enum WebhookEventType
{
    PaymentSucceeded,
    PaymentFailed,
    ChargeRefunded,
    SubscriptionDeleted,
    InvoicePaymentFailed
}