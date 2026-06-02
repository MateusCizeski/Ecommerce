using MediatR;
using FluentValidation;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using Application.Common;
using Application.Exceptions;

namespace Application.Features.Billing.Plans;

/// <summary>
/// Retorna todos os planos ativos disponíveis para novos tenants
/// </summary>
public record GetPlansQuery : IRequest<IEnumerable<GetPlansQueryResult>>;

public record GetPlansQueryResult(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string BillingCycle,
    IReadOnlyCollection<PlanFeatureDto> Features
);

public record PlanFeatureDto(
    string FeatureKey,
    string FeatureName,
    string? LimitValue
);

public class GetPlansQueryHandler(IPlanRepository planRepo) : IRequestHandler<GetPlansQuery, IEnumerable<GetPlansQueryResult>>
{
  public async Task<IEnumerable<GetPlansQueryResult>> Handle(GetPlansQuery query, CancellationToken ct)
  {
    var plans = await planRepo.GetActiveAsync(ct);

    return plans.Select(p => new GetPlansQueryResult(
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.BillingCycle.ToString(),
        p.PlanFeatures
            .Select(pf => new PlanFeatureDto(
                pf.Feature.Key,
                pf.Feature.Name,
                pf.LimitValue
            ))
            .ToList()
    )).ToList();
  }
}

