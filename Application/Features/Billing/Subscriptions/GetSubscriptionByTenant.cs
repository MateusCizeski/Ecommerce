using MediatR;
using Domain.Interfaces;
using Application.Exceptions;
using Ecommerce.Domain;

namespace Application.Features.Billing.Subscriptions;

/// <summary>
/// Retorna a subscription ativa do tenant atual
/// </summary>
public record GetSubscriptionByTenantQuery : IRequest<GetSubscriptionByTenantResult?>;

public record GetSubscriptionByTenantResult(
    Guid SubscriptionId,
    Guid PlanId,
    string PlanName,
    decimal PlanPrice,
    string BillingCycle,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? TrialEndDate,
    bool IsActive
);

public class GetSubscriptionByTenantQueryHandler(
    ISubscriptionRepository subscriptionRepo,
    IHttpTenantContext tenantContext
) : IRequestHandler<GetSubscriptionByTenantQuery, GetSubscriptionByTenantResult?>
{
  public async Task<GetSubscriptionByTenantResult?> Handle(GetSubscriptionByTenantQuery query, CancellationToken ct)
  {
    var tenantId = tenantContext.TenantId;

    var subscription = await subscriptionRepo.GetActiveByTenantAsync(tenantId, ct);
    if (subscription is null)
      return null;

    return new GetSubscriptionByTenantResult(
        subscription.Id,
        subscription.PlanId,
        subscription.Plan.Name,
        subscription.Plan.Price,
        subscription.Plan.BillingCycle.ToString(),
        subscription.Status,
        subscription.StartDate,
        subscription.EndDate,
        subscription.TrialEndDate,
        subscription.IsActive()
    );
  }
}
