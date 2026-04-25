namespace Application.Interfaces;

public interface IPaymentGateway
{
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string customerId, CancellationToken ct = default);
    Task<ConfirmPaymentResult> ConfirmPaymentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(string chargeId, decimal? amount = null, CancellationToken ct = default);
    Task<string> CreateOrGetCustomerAsync(string email, string name, CancellationToken ct = default);
}

public record CreatePaymentIntentResult(string PaymentIntentId, string ClientSecret, bool RequiresAction);
public record ConfirmPaymentResult(bool Succeeded, string ChargeId, string GatewayResponse);
public record RefundResult(bool Succeeded, string RefundId);