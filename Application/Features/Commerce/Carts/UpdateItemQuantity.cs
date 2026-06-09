namespace Application.Features.Commerce.Cart;

public record UpdateCartItemCommand(Guid CartId, Guid VariantId, int Quantity) : IRequest;

public class UpdateCartItemCommandHandler(ICartRepository cartRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<UpdateCartItemCommand>
{
    public async Task Handle(UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct)
            ?? throw new NotFoundException("Carrinho", cmd.CartId);

        if (cart.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        cart.UpdateItemQuantity(cmd.VariantId, cmd.Quantity);

        await uow.CommitAsync(ct);
    }
}