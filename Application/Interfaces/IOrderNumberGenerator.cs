namespace Application.Interfaces;

public interface IOrderNumberGenerator
{
    Task<string> GenerateAsync(Guid tenantId, CancellationToken ct = default);
}
