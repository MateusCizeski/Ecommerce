using Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Commerce.Cart;

public record UpdateCartItemCommand(Guid CartId, Guid VariantId, int Quantity) : IRequest;

public class UpdateCartItemCommandHandler(ICartRepository cartRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<UpdateCartItemCommand>
{
    public async Task Handle(UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct) ?? throw new NotFoundException(nameof(Cart), cmd.CartId);
        if (cart.TenantId != tenant.TenantId) throw new TenantAccessException();
        cart.UpdateItemQuantity(cmd.VariantId, cmd.Quantity);
        await uow.CommitAsync(ct);
    }
}