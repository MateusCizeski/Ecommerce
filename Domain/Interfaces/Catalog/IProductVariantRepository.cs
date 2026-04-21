using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
