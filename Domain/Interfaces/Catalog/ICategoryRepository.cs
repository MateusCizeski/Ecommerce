using Ecommerce.Domain;

namespace Ecommerce.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default);
        Task AddAsync(Category category, CancellationToken ct = default);
        IQueryable<Category> Query(Guid tenantId);
    }
}

