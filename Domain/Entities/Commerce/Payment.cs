namespace Ecommerce.Domain;

public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public decimal RefundableAmount => Amount - RefundedAmount;

    protected Payment() { }

    internal static Payment Create(Guid orderId, PaymentMethod method, decimal amount, string currency)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");

        return new Payment
        {
            OrderId = orderId,
            Method = method,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant()
        };

    public void MarkSucceeded(string chargeId, string gatewayResponse)
    {
        Status = PaymentStatus.Succeeded;
        StripeChargeId = chargeId;
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkFailed(string gatewayResponse)
    {
        Status = PaymentStatus.Failed;
        GatewayResponse = gatewayResponse;
    }

    public void RegisterRefund(decimal refundAmount)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new DomainException("Only succeeded payments can be refunded.");

        if (refundAmount <= 0)
            throw new DomainException("Refund amount must be positive.");

        if (refundAmount > RefundableAmount)
            throw new DomainException(
                $"Refund amount ({refundAmount}) exceeds refundable amount ({RefundableAmount}).");

        RefundedAmount += refundAmount;

        Status = RefundedAmount >= Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        RefundedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void SetStripePaymentIntentId(string id)
    {
        StripePaymentIntentId = id;
        MarkUpdated();
    }
}
