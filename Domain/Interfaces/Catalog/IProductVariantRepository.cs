using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task RestoreStockAsync(Guid variantId, int quantity, CancellationToken ct = default);
}
