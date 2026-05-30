using Application.Interfaces;

namespace Application;

public record ProcessStripeWebhookCommand(
    string Payload,
    string SignatureHeader
) : IRequest<WebhookProcessResult>;

public record WebhookProcessResult(bool Processed, string EventType, string? Reason = null);

public class ProcessStripeWebhookCommandHandler(
    IStripeWebhookEventRepository webhookRepo,
    IOrderRepository orderRepo,
    ISubscriptionRepository subscriptionRepo,
    IPaymentGateway paymentGateway,
    IUnitOfWork uow,
    ICacheService cacheService,
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, WebhookProcessResult>
{
    private static string BuildSubscriptionCacheKey(string tenantId) => $"tenant:{tenantId}:subscription:active";

    public async Task<WebhookProcessResult> Handle(
        ProcessStripeWebhookCommand cmd, CancellationToken ct)
    {
        StripeWebhookParseResult parsed;
        try
        {
            parsed = paymentGateway.ParseWebhookEvent(cmd.Payload, cmd.SignatureHeader);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature");
            throw new DomainException("Invalid webhook signature.");
        }

        if (await webhookRepo.ExistsAsync(parsed.EventId, ct))
        {
            logger.LogInformation(
                "Webhook event {EventId} ({EventType}) already processed — skipping",
                parsed.EventId, parsed.EventType);
            return new WebhookProcessResult(false, parsed.EventType, "Already processed");
        }

        var webhookEvent = StripeWebhookEvent.Create(parsed.EventId, parsed.EventType, parsed.RawPayload);
        await webhookRepo.AddAsync(webhookEvent, ct);
        await uow.CommitAsync(ct);
        try
        {
            await RouteEventAsync(parsed, ct);
            webhookEvent.MarkProcessed();
            await uow.CommitAsync(ct);

            logger.LogInformation(
                "Webhook event {EventId} ({EventType}) processed successfully",
                parsed.EventId, parsed.EventType);

            return new WebhookProcessResult(true, parsed.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process webhook event {EventId} ({EventType})",
                parsed.EventId, parsed.EventType);

            webhookEvent.MarkFailed(ex.Message);
            await uow.CommitAsync(ct);

            throw;
        }
    }

    private async Task RouteEventAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        switch (parsed.EventType)
        {
            case "payment_intent.succeeded":
                await HandlePaymentSucceededAsync(parsed, ct);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(parsed, ct);
                break;

            case "charge.refunded":
                await HandleChargeRefundedAsync(parsed, ct);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(parsed, ct);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(parsed, ct);
                break;

            default:
                logger.LogDebug("Unhandled webhook event type: {EventType}", parsed.EventType);
                break;
        }
    }

    private async Task HandlePaymentSucceededAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        var order = await FindOrderByPaymentIntentAsync(parsed.PaymentIntentId, ct);
        if (order is null)
        {
            logger.LogWarning(
                "No order found for PaymentIntent {PaymentIntentId}", parsed.PaymentIntentId);
            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogInformation(
                "Order {OrderId} is already in status {Status} — skipping webhook confirmation",
                order.Id, order.Status);
            return;
        }

        var payment = order.Payments
            .FirstOrDefault(p => p.StripePaymentIntentId == parsed.PaymentIntentId);

        if (payment is null)
        {
            logger.LogWarning("Payment not found on order {OrderId}", order.Id);
            return;
        }

        payment.MarkSucceeded(parsed.ChargeId ?? string.Empty, "webhook:payment_intent.succeeded");
        order.ConfirmPayment();
        await uow.CommitAsync(ct);
    }

    private async Task HandlePaymentFailedAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        var order = await FindOrderByPaymentIntentAsync(parsed.PaymentIntentId, ct);
        if (order is null) return;

        var payment = order.Payments
            .FirstOrDefault(p => p.StripePaymentIntentId == parsed.PaymentIntentId);

        if (payment is null) return;

        payment.MarkFailed(parsed.FailureMessage ?? "Payment failed");
        await uow.CommitAsync(ct);
    }

    private async Task HandleChargeRefundedAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        if (parsed.ChargeId is null) return;

        var order = await orderRepo.GetByChargeIdAsync(parsed.ChargeId, ct);
        if (order is null)
        {
            logger.LogWarning("No order found for charge {ChargeId}", parsed.ChargeId);
            return;
        }

        var payment = order.Payments
            .FirstOrDefault(p => p.StripeChargeId == parsed.ChargeId);

        if (payment is null) return;

        var refundAmount = parsed.Amount ?? payment.Amount;
        payment.RegisterRefund(refundAmount);
        await uow.CommitAsync(ct);
    }

    private async Task HandleSubscriptionDeletedAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parsed.StripeSubscriptionId)) return;

        var subscription = await subscriptionRepo.GetByStripeSubscriptionIdAsync(parsed.StripeSubscriptionId, ct);
        if (subscription is null)
        {
            logger.LogWarning("No subscription found for Stripe subscription {StripeSubscriptionId}", parsed.StripeSubscriptionId);
            return;
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
            return;

        subscription.Cancel();
        await ClearSubscriptionCacheAsync(subscription.TenantId, ct);
        await uow.CommitAsync(ct);
    }

    private async Task HandleInvoicePaymentFailedAsync(StripeWebhookParseResult parsed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parsed.StripeSubscriptionId)) return;

        var subscription = await subscriptionRepo.GetByStripeSubscriptionIdAsync(parsed.StripeSubscriptionId, ct);
        if (subscription is null) return;

        if (subscription.Status == SubscriptionStatus.Cancelled || subscription.Status == SubscriptionStatus.Expired)
            return;

        subscription.MarkPastDue();
        await ClearSubscriptionCacheAsync(subscription.TenantId, ct);
        await uow.CommitAsync(ct);
    }

    private async Task ClearSubscriptionCacheAsync(Guid tenantId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(BuildSubscriptionCacheKey(tenantId.ToString()), ct);
    }

    private async Task<Order?> FindOrderByPaymentIntentAsync(string paymentIntentId, CancellationToken ct)
        => await orderRepo.GetByPaymentIntentIdAsync(paymentIntentId, ct);
}
