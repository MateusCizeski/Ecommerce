namespace Ecommerce.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception inner) : base(message, inner) { }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string entity, object key) : base($"Entity '{entity}' with key '{key}' was not found.") { }
    }

    public class TenantAccessException : DomainException
    {
        public TenantAccessException() : base("Access denied: resource does not belong to the current tenant.") { }
    }
}
