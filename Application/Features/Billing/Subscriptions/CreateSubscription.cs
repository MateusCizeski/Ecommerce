using MediatR;
using FluentValidation;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using Application.Exceptions;
using Application.Interfaces;
using ValidationException = FluentValidation.ValidationException;

namespace Application.Features.Billing.Subscriptions;

/// <summary>
/// Cria uma nova subscription para um tenant em um plano específico
/// </summary>
public record CreateSubscriptionCommand(
    Guid PlanId,
    int TrialDays = 0
) : IRequest<CreateSubscriptionResult>;

public record CreateSubscriptionResult(
    Guid SubscriptionId,
    Guid PlanId,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? TrialEndDate,
    string? StripeSubscriptionId
);

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
  public CreateSubscriptionCommandValidator()
  {
    RuleFor(x => x.PlanId)
        .NotEqual(Guid.Empty)
        .WithMessage("Plan ID inválido.");

    RuleFor(x => x.TrialDays)
        .GreaterThanOrEqualTo(0)
        .LessThanOrEqualTo(90)
        .WithMessage("Dias de trial devem estar entre 0 e 90.");
  }
}

public class CreateSubscriptionCommandHandler(
    IPlanRepository planRepo,
    ISubscriptionRepository subscriptionRepo,
    ITenantRepository tenantRepo,
    ITenantContext tenantContext,
    IPaymentGateway paymentGateway,
    IUnitOfWork uow,
    ICacheService cacheService
) : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
  private static string BuildSubscriptionCacheKey(Guid tenantId) => $"tenant:{tenantId}:subscription:active";

  public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
  {
    var tenantId = tenantContext.TenantId;
    var tenant = await tenantRepo.GetByIdAsync(tenantId, ct)
                 ?? throw new NotFoundException("Tenant", tenantId);

    // 1. Verifica se o plano existe
    var plan = await planRepo.GetByIdAsync(cmd.PlanId, ct)
            ?? throw new NotFoundException("Plan", cmd.PlanId);

    if (!plan.IsActive)
      throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("PlanId", "O plano selecionado não está disponível.") });

    // 2. Verifica se o tenant já tem uma subscription ativa
    var existingSubscription = await subscriptionRepo.GetActiveByTenantAsync(tenantId, ct);
    if (existingSubscription is not null)
      throw new ConflictException("O tenant já possui uma subscription ativa. Cancele a anterior primeiro.");

    // 3. Cria ou recupera o Stripe Customer e a Stripe Subscription
    var stripeCustomerId = await paymentGateway.CreateOrGetCustomerAsync(tenant.Email, tenant.Name, ct);
    var stripeSubscriptionId = await paymentGateway.CreateStripeSubscriptionAsync(
        stripeCustomerId,
        plan.Name,
        plan.Price,
        plan.BillingCycle,
        ct);

    // 4. Cria a subscription local
    var now = DateTime.UtcNow;
    var startDate = now;

    var endDate = plan.BillingCycle switch
    {
      BillingCycle.Yearly => now.AddYears(1),
      _ => now.AddMonths(1)
    };

    DateTime? trialEndDate = cmd.TrialDays > 0 ? now.AddDays(cmd.TrialDays) : null;

    var subscription = Subscription.Create(
        tenantId,
        plan,
        startDate,
        endDate,
        trialEndDate
    );
    subscription.SetStripeId(stripeSubscriptionId);

    await subscriptionRepo.AddAsync(subscription, ct);
    await uow.CommitAsync(ct);
    await cacheService.RemoveAsync(BuildSubscriptionCacheKey(tenantId), ct);

    return new CreateSubscriptionResult(
        subscription.Id,
        subscription.PlanId,
        subscription.Status,
        subscription.StartDate,
        subscription.EndDate,
        subscription.TrialEndDate,
        subscription.StripeSubscriptionId
    );
  }
}

