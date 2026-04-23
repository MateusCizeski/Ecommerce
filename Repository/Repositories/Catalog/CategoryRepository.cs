using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Category category, CancellationToken ct = default)
        => await _db.Categories.AddAsync(category, ct);

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id, ct);

    public IQueryable<Category> Query(Guid tenantId)
        => _db.Categories
              .Include(c => c.ParentCategory)
              .Where(c => c.TenantId == tenantId);

    public async Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default)
        => await _db.Categories.AnyAsync(c => c.TenantId == tenantId && c.Slug == slug, ct);
}
