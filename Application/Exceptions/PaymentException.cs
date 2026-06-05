namespace Application.Exceptions;

public class PaymentException : Exception
{
    public string? GatewayCode { get; }

    public PaymentException(string message, string? gatewayCode = null) : base(message)
    {
        GatewayCode = gatewayCode;
    }

    public PaymentException(string message, Exception innerException, string? gatewayCode = null) : base(message, innerException)
    {
        GatewayCode = gatewayCode;
    }
}
