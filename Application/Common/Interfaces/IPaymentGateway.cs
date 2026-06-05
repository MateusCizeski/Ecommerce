using MediatR;

namespace Application.Common.Interfaces.Payments;

// Focada exclusivamente em pagamentos avulsos
public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, string customerId,
        CancellationToken ct = default);

    Task<PaymentConfirmationResult> ConfirmPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default);

    Task<RefundResult> RefundAsync(
        string chargeId, decimal? amount = null,
        CancellationToken ct = default);

    Task<string> CreateOrGetCustomerAsync(
        string email, string name,
        CancellationToken ct = default);
}

// Focada exclusivamente em assinaturas / recorrência (ISP)
public interface IBillingSubscriptionService
{
    Task<string> CreateSubscriptionAsync(
        string customerId,
        string planName,
        decimal amount,
        BillingCycle billingCycle,
        CancellationToken ct = default);

    Task<bool> CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken ct = default);
}

// Focada estritamente no parser de notificações da infraestrutura
public interface IPaymentWebhookParser
{
    WebhookParseResult ParseEvent(string payload, string signatureHeader);
}