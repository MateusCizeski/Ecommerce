namespace Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Você não tem permissão para executar esta ação.") { }

    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}
