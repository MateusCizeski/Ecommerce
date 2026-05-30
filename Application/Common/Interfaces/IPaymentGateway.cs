namespace Application;

public interface IPaymentGateway
{
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, string stripeCustomerId,
        CancellationToken ct = default);

    Task<ConfirmPaymentResult> ConfirmPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default);

    Task<RefundResult> RefundAsync(
        string chargeId, decimal? amount = null,
        CancellationToken ct = default);

    Task<string> CreateOrGetCustomerAsync(
        string email, string name,
        CancellationToken ct = default);

    Task<string> CreateStripeSubscriptionAsync(
        string stripeCustomerId,
        string planName,
        decimal amount,
        BillingCycle billingCycle,
        CancellationToken ct = default);

    Task<bool> CancelStripeSubscriptionAsync(
        string stripeSubscriptionId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the Stripe-Signature header and deserializes the event.
    /// Throws if the signature is invalid (tampered or wrong secret).
    /// Returns the raw event type and the event id for idempotency checks.
    /// </summary>
    StripeWebhookParseResult ParseWebhookEvent(string payload, string signatureHeader);
}

public record CreatePaymentIntentResult(string PaymentIntentId, string ClientSecret, bool RequiresAction);
public record ConfirmPaymentResult(bool Succeeded, string ChargeId, string GatewayResponse);
public record RefundResult(bool Succeeded, string RefundId, decimal Amount);

public record StripeWebhookParseResult(
    string EventId,
    string EventType,
    string PaymentIntentId,
    string? ChargeId,
    string? StripeSubscriptionId,
    string? CustomerId,
    decimal? Amount,
    string? FailureMessage,
    string RawPayload
);
