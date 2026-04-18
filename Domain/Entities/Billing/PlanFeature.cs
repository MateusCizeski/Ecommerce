namespace Ecommerce.Domain;

public class PlanFeature : BaseEntity
{
    public Guid PlanId { get; private set; }
    public Guid FeatureId { get; private set; }
    public string? LimitValue { get; private set; }

    public Plan Plan { get; private set; } = default!;
    public Feature Feature { get; private set; } = default!;

    protected PlanFeature() { }

    internal static PlanFeature Create(Guid planId, Guid featureId, string? limitValue) => new()
    {
        PlanId = planId,
        FeatureId = featureId,
        LimitValue = limitValue
    };
}
