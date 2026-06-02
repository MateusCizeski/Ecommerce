using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _db;

        public ProductRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(Product product, CancellationToken ct = default)
            => await _db.Products.AddAsync(product, ct);

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Products
                        .Include(p => p.Category)
                        .Include(p => p.Variants).ThenInclude(v => v.Attributes)
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        public IQueryable<Product> Query(Guid tenantId)
            => _db.Products
                  .Include(p => p.Category)
                  .Where(p => p.TenantId == tenantId);

        public async Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default)
            => await _db.Products.AnyAsync(p => p.TenantId == tenantId && p.Slug == slug, ct);
    }
}

