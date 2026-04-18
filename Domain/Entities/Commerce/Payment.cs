namespace Ecommerce.Domain;

public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    protected Payment() { }

    internal static Payment Create(Guid orderId, PaymentMethod method, decimal amount, string currency) => new()
    {
        OrderId = orderId,
        Method = method,
        Amount = amount,
        Currency = currency
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

    public void Refund()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new DomainException("Only succeeded payments can be refunded.");
        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
    }

    public void SetStripePaymentIntentId(string id) => StripePaymentIntentId = id;
}
