namespace Application.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("You do not have permission to perform this action.") { }
    public ForbiddenException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class PaymentException : Exception
{
    public string? GatewayCode { get; }
    public PaymentException(string message, string? gatewayCode = null)
        : base(message) => GatewayCode = gatewayCode;
}