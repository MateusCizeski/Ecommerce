using Application.Features.Billing.Plans.DTOs;

namespace Application.Features.Billing.Plans;

public record GetPlanByIdQuery(Guid PlanId) : IRequest<PlanDetailDto>;

public record PlanDetailDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string BillingCycle,
    bool IsActive,
    IReadOnlyCollection<PlanFeatureDto> Features
);

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

public class GetPlanByIdQueryHandler(IPlanRepository planRepo)
    : IRequestHandler<GetPlanByIdQuery, PlanDetailDto>
{
    public async Task<PlanDetailDto> Handle(GetPlanByIdQuery query, CancellationToken ct)
    {
        var plan = await planRepo.GetByIdAsync(query.PlanId, ct)
            ?? throw new NotFoundException("Plan", query.PlanId);

        return new PlanDetailDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.BillingCycle.ToString(),
            plan.IsActive,
            plan.PlanFeatures
                .Select(pf => new PlanFeatureDto(pf.Feature.Id, pf.Feature.Key, pf.Feature.Name, pf.LimitValue))
                .ToList()
        );
    }
}

