using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories
{
    internal class ProductVariantRepository : IProductVariantRepository
    {
        private readonly AppDbContext _db;

        public ProductVariantRepository(AppDbContext db) => _db = db;

        public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == id, ct);

        public async Task RestoreStockAsync(Guid variantId, int quantity, CancellationToken ct = default)
        {
            var variant = await _db.ProductVariants.FindAsync(new object[] { variantId }, ct);
            if (variant is null)
                throw new KeyNotFoundException($"Product variant '{variantId}' not found.");

            variant.AddStock(quantity, "Refund restored stock");
        }
    }
}
