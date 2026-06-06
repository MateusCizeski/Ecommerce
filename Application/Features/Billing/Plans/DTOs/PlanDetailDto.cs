namespace Application.Features.Billing.Plans.DTOs
{
    public record PlanDetailDto(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        string BillingCycle,
        bool IsActive,
        IReadOnlyCollection<PlanFeatureDto> Features
    );
}