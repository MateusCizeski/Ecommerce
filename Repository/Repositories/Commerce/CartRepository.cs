using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Cart cart, CancellationToken ct = default)
        => await _db.Carts.AddAsync(cart, ct);

    public async Task<Cart?> GetActiveByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct = default)
        => await _db.Carts.Include(c => c.Items)
                          .FirstOrDefaultAsync(c =>
                              c.TenantId == tenantId &&
                              c.CustomerId == customerId &&
                              c.Status == CartStatus.Active, ct);

    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Carts.Include(c => c.Items)
                          .FirstOrDefaultAsync(c => c.Id == id, ct);
}

