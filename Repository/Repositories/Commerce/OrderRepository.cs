using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await _db.Orders.AddAsync(order, ct);

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == id, ct);

    public IQueryable<Order> Query(Guid tenantId)
        => _db.Orders.Include(o => o.Items)
                     .Where(o => o.TenantId == tenantId);
}
