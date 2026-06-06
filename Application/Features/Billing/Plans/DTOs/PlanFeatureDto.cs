namespace Application.Features.Billing.Plans.DTOs
{
    public record PlanFeatureDto(
        Guid? FeatureId,
        string FeatureKey,
        string FeatureName,
        string? LimitValue
    );
}