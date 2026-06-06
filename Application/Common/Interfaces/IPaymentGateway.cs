namespace Application.Common.Interfaces.Payments;

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

public interface IPaymentWebhookParser
{
    WebhookParseResult ParseEvent(string payload, string signatureHeader);
}