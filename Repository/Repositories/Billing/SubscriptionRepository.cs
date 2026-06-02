using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _db;
    public SubscriptionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        => await _db.Subscriptions.AddAsync(subscription, ct);

    public async Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Subscriptions
                    .Include(s => s.Plan).ThenInclude(p => p.PlanFeatures).ThenInclude(pf => pf.Feature)
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId
                        && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
                        && s.EndDate > now, ct);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default)
        => await _db.Subscriptions
                    .Include(s => s.Plan).ThenInclude(p => p.PlanFeatures).ThenInclude(pf => pf.Feature)
                    .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
}

