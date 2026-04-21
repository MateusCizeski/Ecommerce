using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    IQueryable<Product> Query(Guid tenantId);
}
