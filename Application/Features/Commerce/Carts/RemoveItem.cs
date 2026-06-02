using MediatR;
using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;

namespace Application.Features.Commerce.Cart;

public record RemoveCartItemCommand(Guid CartId, Guid VariantId) : IRequest;

public class RemoveCartItemCommandHandler(ICartRepository cartRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct) ?? throw new NotFoundException(nameof(Cart), cmd.CartId);
        if (cart.TenantId != tenant.TenantId) throw new TenantAccessException();
        cart.RemoveItem(cmd.VariantId);
        await uow.CommitAsync(ct);
    }
}
