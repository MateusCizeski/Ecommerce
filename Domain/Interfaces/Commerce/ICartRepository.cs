using Ecommerce.Domain;

namespace Ecommerce.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cart?> GetActiveByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
}

