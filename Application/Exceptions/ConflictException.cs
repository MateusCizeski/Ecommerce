namespace Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException() : base("Ocorreu um conflito com o estado atual do recurso.") { }

    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}
