using MediatR;
using Ecommerce.Domain.Interfaces;
using Application.Exceptions;
using Application.Interfaces;

namespace Application.Features.Billing.Subscriptions;

/// <summary>
/// Cancela a subscription ativa do tenant atual.
/// </summary>
public record CancelSubscriptionCommand : IRequest<CancelSubscriptionResult>;

public record CancelSubscriptionResult(
    bool Cancelled,
    bool StripeCancelled,
    DateTime CancelledAt
);

public class CancelSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepo,
    ITenantContext tenantContext,
    IPaymentGateway paymentGateway,
    IUnitOfWork uow,
    ICacheService cacheService)
    : IRequestHandler<CancelSubscriptionCommand, CancelSubscriptionResult>
{
  private static string BuildSubscriptionCacheKey(Guid tenantId) => $"tenant:{tenantId}:subscription:active";

  public async Task<CancelSubscriptionResult> Handle(CancelSubscriptionCommand cmd, CancellationToken ct)
  {
    var tenantId = tenantContext.TenantId;
    var subscription = await subscriptionRepo.GetActiveByTenantAsync(tenantId, ct)
        ?? throw new NotFoundException("Subscription", tenantId);

    var stripeCancelled = false;
    if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
    {
      stripeCancelled = await paymentGateway.CancelStripeSubscriptionAsync(subscription.StripeSubscriptionId, ct);
    }

    subscription.Cancel();
    await uow.CommitAsync(ct);
    await cacheService.RemoveAsync(BuildSubscriptionCacheKey(tenantId), ct);

    return new CancelSubscriptionResult(true, stripeCancelled, subscription.CancelledAt ?? DateTime.UtcNow);
  }
}

