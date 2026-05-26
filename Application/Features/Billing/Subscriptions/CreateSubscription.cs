using MediatR;
using FluentValidation;
using Domain.Interfaces;
using Ecommerce.Domain;
using Application.Exceptions;
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
    DateTime? TrialEndDate
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
    ITenantContext tenantContext,
    IUnitOfWork uow
) : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
  public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
  {
    var tenantId = tenantContext.TenantId;

    // 1. Verifica se o plano existe
    var plan = await planRepo.GetByIdAsync(cmd.PlanId, ct)
            ?? throw new NotFoundException("Plan", cmd.PlanId);

    if (!plan.IsActive)
      throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("PlanId", "O plano selecionado não está disponível.") });
    // 2. Verifica se o tenant já tem uma subscription ativa
    var existingSubscription = await subscriptionRepo.GetActiveByTenantAsync(tenantId, ct);
    if (existingSubscription is not null)
      throw new ConflictException("O tenant já possui uma subscription ativa. Cancele a anterior primeiro.");

    // 3. Cria a subscription
    var now = DateTime.UtcNow;
    var startDate = now;

    // Define a data de término com base no ciclo de cobrança do plano
    var endDate = plan.BillingCycle switch
    {
      BillingCycle.Yearly => now.AddYears(1),
      _ => now.AddMonths(1)
    };

    // Define data de fim do trial se houver
    DateTime? trialEndDate = cmd.TrialDays > 0 ? now.AddDays(cmd.TrialDays) : null;

    var subscription = Subscription.Create(
        tenantId,
        plan,
        startDate,
        endDate,
        trialEndDate
    );

    // 4. Persiste
    await subscriptionRepo.AddAsync(subscription, ct);
    await uow.CommitAsync(ct);

    return new CreateSubscriptionResult(
        subscription.Id,
        subscription.PlanId,
        subscription.Status,
        subscription.StartDate,
        subscription.EndDate,
        subscription.TrialEndDate
    );
  }
}
