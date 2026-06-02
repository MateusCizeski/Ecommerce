using MediatR;
using Ecommerce.Domain.Interfaces;
using Application.Exceptions;

namespace Application.Features.Billing.Plans;

/// <summary>
/// Retorna detalhe de um plano específico com suas features
/// </summary>
public record GetPlanByIdQuery(Guid PlanId) : IRequest<GetPlanByIdQueryResult>;

public record GetPlanByIdQueryResult(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string BillingCycle,
    bool IsActive,
    IReadOnlyCollection<PlanFeatureDetailDto> Features
);

public record PlanFeatureDetailDto(
    Guid FeatureId,
    string FeatureKey,
    string FeatureName,
    string? LimitValue
);

public class GetPlanByIdQueryHandler(IPlanRepository planRepo) : IRequestHandler<GetPlanByIdQuery, GetPlanByIdQueryResult>
{
  public async Task<GetPlanByIdQueryResult> Handle(GetPlanByIdQuery query, CancellationToken ct)
  {
    var plan = await planRepo.GetByIdAsync(query.PlanId, ct)
            ?? throw new NotFoundException("Plan", query.PlanId);
    return new GetPlanByIdQueryResult(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.Price,
        plan.BillingCycle.ToString(),
        plan.IsActive,
        plan.PlanFeatures
            .Select(pf => new PlanFeatureDetailDto(
                pf.Feature.Id,
                pf.Feature.Key,
                pf.Feature.Name,
                pf.LimitValue
            ))
            .ToList()
    );
  }
}

