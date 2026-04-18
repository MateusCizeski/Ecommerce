namespace Ecommerce.Domain;

public class Plan : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;
    public BillingCycle BillingCycle { get; private set; }
    private readonly List<PlanFeature> _planFeatures = [];
    public IReadOnlyCollection<PlanFeature> PlanFeatures => _planFeatures.AsReadOnly();

    protected Plan() { }

    public static Plan Create(string name, string description, decimal price, BillingCycle billingCycle)
    {
        if (price < 0) throw new DomainException("Price cannot be negative.");
        return new Plan { Name = name.Trim(), Description = description.Trim(), Price = price, BillingCycle = billingCycle };
    }

    public void AddFeature(Feature feature, string? limitValue = null)
    {
        if (_planFeatures.Any(pf => pf.FeatureId == feature.Id))
            throw new DomainException($"Feature '{feature.Key}' is already assigned to this plan.");
        _planFeatures.Add(PlanFeature.Create(Id, feature.Id, limitValue));
    }
}
