using Ecommerce.Domain;

namespace Domain;

public interface IStripeWebhookEventRepository
{
    Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct = default);
    Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken ct = default);
    Task<StripeWebhookEvent?> GetByStripeEventIdAsync(string stripeEventId, CancellationToken ct = default);
}

