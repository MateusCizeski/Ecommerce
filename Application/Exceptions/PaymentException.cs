namespace Application.Exceptions;

public class PaymentException : Exception
{
    public string? GatewayCode { get; }
    public PaymentException(string message, string? gatewayCode = null)
        : base(message) => GatewayCode = gatewayCode;
}
