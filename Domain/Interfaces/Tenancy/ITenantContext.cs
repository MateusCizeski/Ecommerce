namespace Domain.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    string Subdomain { get; }
}
