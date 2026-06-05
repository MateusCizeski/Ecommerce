namespace Application.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Ocorreram um ou mais erros de validação.")
    {
        Errors = new Dictionary<string, string[]>().AsReadOnly();
    }

    // Desacoplado do FluentValidation! Aceita um dicionário genérico estruturado.
    public ValidationException(IDictionary<string, string[]> errors)
        : base("Ocorreram um ou mais erros de validação.")
    {
        Errors = errors.AsReadOnly();
    }
}
