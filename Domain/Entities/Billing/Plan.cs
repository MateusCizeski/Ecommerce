namespace Ecommerce.Domain;

public class Plan : BaseEntity
{
  public string Name { get; private set; } = default!;
  public string Description { get; private set; } = default!;
  public decimal Price { get; private set; }
  public BillingCycle BillingCycle { get; private set; }
  public bool IsActive { get; private set; } = true;

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

public class Feature : BaseEntity
{
  public string Key { get; private set; } = default!;
  public string Name { get; private set; } = default!;
  public string Description { get; private set; } = default!;

  protected Feature() { }

  public static Feature Create(string key, string name, string description) => new()
  {
    Key = key.Trim().ToLowerInvariant(),
    Name = name.Trim(),
    Description = description.Trim()
  };
}

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

public class Subscription : BaseEntity
{
  public Guid TenantId { get; private set; }
  public Guid PlanId { get; private set; }
  public SubscriptionStatus Status { get; private set; }
  public DateTime StartDate { get; private set; }
  public DateTime EndDate { get; private set; }
  public DateTime? TrialEndDate { get; private set; }
  public DateTime? CancelledAt { get; private set; }
  public string? StripeSubscriptionId { get; private set; }

  public Plan Plan { get; private set; } = default!;

  protected Subscription() { }

  public static Subscription Create(Guid tenantId, Plan plan, DateTime startDate, DateTime endDate, DateTime? trialEndDate = null)
  {
    if (endDate <= startDate) throw new DomainException("End date must be after start date.");

    var sub = new Subscription
    {
      TenantId = tenantId,
      PlanId = plan.Id,
      Status = trialEndDate.HasValue ? SubscriptionStatus.Trialing : SubscriptionStatus.Active,
      StartDate = startDate,
      EndDate = endDate,
      TrialEndDate = trialEndDate
    };
    sub.AddDomainEvent(new SubscriptionCreatedEvent(sub.Id, tenantId, plan.Id));
    return sub;
  }

  public void Cancel()
  {
    if (Status == SubscriptionStatus.Cancelled)
      throw new DomainException("Subscription is already cancelled.");
    Status = SubscriptionStatus.Cancelled;
    CancelledAt = DateTime.UtcNow;
    MarkUpdated();
    AddDomainEvent(new SubscriptionCancelledEvent(Id, TenantId));
  }

  public void SetStripeId(string id) { StripeSubscriptionId = id; MarkUpdated(); }
  public bool IsActive() => Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing && EndDate > DateTime.UtcNow;
}