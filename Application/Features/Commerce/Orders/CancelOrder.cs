namespace Application.Features.Commerce.Orders;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("O ID do pedido é obrigatório.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("A justificativa é obrigatória e pode conter até 500 caracteres.");
    }
}

public class CancelOrderCommandHandler(IOrderRepository orderRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new NotFoundException("Pedido", cmd.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        order.Cancel(cmd.Reason);
        await uow.CommitAsync(ct);
    }
}