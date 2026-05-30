using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default);
    Task AddAsync(Subscription subscription, CancellationToken ct = default);
}
