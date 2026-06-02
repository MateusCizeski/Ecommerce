using Ecommerce.Domain;

namespace Ecommerce.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    IQueryable<Order> Query(Guid tenantId);
    Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task<Order?> GetByChargeIdAsync(string chargeId, CancellationToken ct = default);
}

