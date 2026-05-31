using Ecommerce.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecommerce.Test.Domain;

[TestClass]
public sealed class SubscriptionTests
{
  [TestMethod]
  public void Create_WithValidDates_ShouldBeActiveAndRaiseEvent()
  {
    var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
    var startDate = DateTime.UtcNow;
    var endDate = startDate.AddDays(30);

    var subscription = Subscription.Create(Guid.NewGuid(), plan, startDate, endDate);

    subscription.Status.Should().Be(SubscriptionStatus.Active);
    subscription.IsActive().Should().BeTrue();
    subscription.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SubscriptionCreatedEvent>();
  }

  [TestMethod]
  public void Create_WithInvalidDates_ShouldThrowDomainException()
  {
    var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
    var startDate = DateTime.UtcNow;
    var endDate = startDate;

    Action action = () => Subscription.Create(Guid.NewGuid(), plan, startDate, endDate);

    action.Should().Throw<DomainException>().WithMessage("End date must be after start date.");
  }

  [TestMethod]
  public void Cancel_ShouldSetStatusCancelledAndRaiseEvent()
  {
    var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
    var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

    subscription.Cancel();

    subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    subscription.CancelledAt.Should().NotBeNull();
    subscription.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SubscriptionCancelledEvent>();
  }

  [TestMethod]
  public void MarkPastDue_WhenCancelled_ShouldThrowDomainException()
  {
    var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
    var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
    subscription.Cancel();

    Action action = () => subscription.MarkPastDue();

    action.Should().Throw<DomainException>().WithMessage("Cannot mark a cancelled subscription as past due.");
  }

  [TestMethod]
  public void SetStripeId_WithEmptyValue_ShouldThrowDomainException()
  {
    var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
    var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

    Action action = () => subscription.SetStripeId(string.Empty);

    action.Should().Throw<DomainException>().WithMessage("Stripe subscription ID cannot be empty.");
  }
}
