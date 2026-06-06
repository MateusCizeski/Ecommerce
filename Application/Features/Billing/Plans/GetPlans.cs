using Application.Features.Billing.Plans.DTOs;

namespace Application.Features.Billing.Plans;

public record GetPlansQuery : IRequest<IReadOnlyCollection<PlanDetailDto>>;

public record GetPlansQueryResult(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string BillingCycle,
    IReadOnlyCollection<PlanFeatureDto> Features
);

public class GetPlansQueryHandler(IPlanRepository planRepo)
    : IRequestHandler<GetPlansQuery, IReadOnlyCollection<PlanDetailDto>>
{
    public async Task<IReadOnlyCollection<PlanDetailDto>> Handle(GetPlansQuery query, CancellationToken ct)
    {
        var plans = await planRepo.GetActiveAsync(ct);

        return plans.Select(p => new PlanDetailDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.BillingCycle.ToString(),
            p.IsActive,
            p.PlanFeatures
                .Select(pf => new PlanFeatureDto(null, pf.Feature.Key, pf.Feature.Name, pf.LimitValue))
                .ToList()
        )).ToList().AsReadOnly();
    }
}

