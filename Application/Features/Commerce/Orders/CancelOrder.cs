using MediatR;
using Ecommerce.Domain;
using FluentValidation;
using Domain.Interfaces;

namespace Application.Features.Commerce.Orders;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator() { RuleFor(x => x.OrderId).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); }
}

public class CancelOrderCommandHandler(IOrderRepository orderRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct) ?? throw new NotFoundException(nameof(Order), cmd.OrderId);
        if (order.TenantId != tenant.TenantId) throw new TenantAccessException();
        order.Cancel(cmd.Reason);
        await uow.CommitAsync(ct);
    }
}