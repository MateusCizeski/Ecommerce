using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories
{
    internal class ProductVariantRepository : IProductVariantRepository
    {
        private readonly AppDbContext _db;
        public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == id, ct);
    }
}
