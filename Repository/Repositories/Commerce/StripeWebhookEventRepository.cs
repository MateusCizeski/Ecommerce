using Ecommerce.Domain;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class StripeWebhookEventRepository : IStripeWebhookEventRepository
{
  private readonly AppDbContext _db;

  public StripeWebhookEventRepository(AppDbContext db) => _db = db;

  public async Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct = default)
      => await _db.StripeWebhookEvents
          .AsNoTracking()
          .AnyAsync(e => e.StripeEventId == stripeEventId, ct);

  public async Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken ct = default)
      => await _db.StripeWebhookEvents.AddAsync(webhookEvent, ct);

  public async Task<StripeWebhookEvent?> GetByStripeEventIdAsync(string stripeEventId, CancellationToken ct = default)
      => await _db.StripeWebhookEvents
          .AsNoTracking()
          .FirstOrDefaultAsync(e => e.StripeEventId == stripeEventId, ct);
}
