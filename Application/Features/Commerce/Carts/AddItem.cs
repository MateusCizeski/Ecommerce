using Application.Features.Commerce.Cart.DTOs;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application.Features.Commerce.Cart;

public record AddCartItemCommand(Guid CartId, Guid VariantId, int Quantity) : IRequest<CartDto>;

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator() { RuleFor(x => x.CartId).NotEmpty(); RuleFor(x => x.VariantId).NotEmpty(); RuleFor(x => x.Quantity).GreaterThan(0); }
}

public class AddCartItemCommandHandler(ICartRepository cartRepo, IProductVariantRepository variantRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct) ?? throw new NotFoundException(nameof(Cart), cmd.CartId);
        var variant = await variantRepo.GetByIdAsync(cmd.VariantId, ct) ?? throw new NotFoundException(nameof(ProductVariant), cmd.VariantId);
        if (cart.TenantId != tenant.TenantId) throw new TenantAccessException();
        cart.AddItem(variant, cmd.Quantity);
        await uow.CommitAsync(ct);
        return new CartDto(cart.Id, cart.CustomerId, cart.Total, cart.ItemCount,
            cart.Items.Select(i => new CartItemDto(i.Id, i.ProductVariantId, string.Empty, string.Empty, i.Quantity, i.UnitPrice, i.LineTotal)),
            cart.Status.ToString(), cart.ExpiresAt);
    }
}