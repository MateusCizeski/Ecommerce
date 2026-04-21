using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _db;
    public TenantRepository(AppDbContext db) => _db = db;

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Tenants.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
        => await _db.Tenants.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);

    public async Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct = default)
        => await _db.Tenants.IgnoreQueryFilters()
                            .AnyAsync(t => t.Subdomain == subdomain && t.DeletedAt == null, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => await _db.Tenants.AddAsync(tenant, ct);
}
