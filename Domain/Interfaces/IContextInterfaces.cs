namespace Ecommerce.Domain.Interfaces
{
    public interface ITenantContext
    {
        Guid TenantId { get; }
        string Subdomain { get; }
    }

    public interface ICurrentUser
    {
        Guid? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
    }
}
