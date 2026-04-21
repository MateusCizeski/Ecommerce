using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    IQueryable<Order> Query(Guid tenantId);
}
