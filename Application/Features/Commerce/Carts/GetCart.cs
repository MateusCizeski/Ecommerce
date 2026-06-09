using Application.Features.Commerce.Cart.DTOs;

namespace Application.Features.Commerce.Cart;

public class GetOrCreateCartCommandHandler(
    ICartRepository cartRepo,
    ICustomerRepository customerRepo,
    IUnitOfWork uow,
    ITenantContext tenant) : IRequestHandler<GetOrCreateCartCommand, CartDto>
{
    public async Task<CartDto> Handle(GetOrCreateCartCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetActiveByCustomerAsync(tenant.TenantId, cmd.CustomerId, ct);

        if (cart is null)
        {
            _ = await customerRepo.GetByIdAsync(cmd.CustomerId, ct)
                ?? throw new NotFoundException("Cliente", cmd.CustomerId);

            cart = Ecommerce.Domain.Cart.Create(tenant.TenantId, cmd.CustomerId);

            await cartRepo.AddAsync(cart, ct);
            await uow.CommitAsync(ct);
        }

        return cart.ToDto();
    }
}

