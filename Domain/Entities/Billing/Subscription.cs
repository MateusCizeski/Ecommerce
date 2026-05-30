namespace Ecommerce.Domain;

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
        if (Status == SubscriptionStatus.Cancelled) throw new DomainException("Subscription is already cancelled.");
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new SubscriptionCancelledEvent(Id, TenantId));
    }

    public void MarkPastDue()
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new DomainException("Cannot mark a cancelled subscription as past due.");

        if (Status == SubscriptionStatus.PastDue)
            return;

        Status = SubscriptionStatus.PastDue;
        MarkUpdated();
    }

    public void SetStripeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new DomainException("Stripe subscription ID cannot be empty.");
        StripeSubscriptionId = id;
        MarkUpdated();
    }

    public bool IsActive()
        => (Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing) && EndDate > DateTime.UtcNow;
}
