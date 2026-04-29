using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application;

public record CartDto(Guid Id, Guid CustomerId, decimal Total, int ItemCount, IEnumerable<CartItemDto> Items, string Status, DateTime? ExpiresAt);
public record CartItemDto(Guid Id, Guid VariantId, string VariantName, string SKU, int Quantity, decimal UnitPrice, decimal LineTotal);

public record GetOrCreateCartCommand(Guid CustomerId) : IRequest<CartDto>;

public class GetOrCreateCartCommandHandler(ICartRepository cartRepo, ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<GetOrCreateCartCommand, CartDto>
{
    public async Task<CartDto> Handle(GetOrCreateCartCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetActiveByCustomerAsync(tenant.TenantId, cmd.CustomerId, ct);
        if (cart is null)
        {
            _ = await customerRepo.GetByIdAsync(cmd.CustomerId, ct) ?? throw new NotFoundException("Customer", cmd.CustomerId);
            cart = Cart.Create(tenant.TenantId, cmd.CustomerId);
            await cartRepo.AddAsync(cart, ct);
            await uow.CommitAsync(ct);
        }
        return ToDto(cart);
    }

    private static CartDto ToDto(Cart cart) => new(cart.Id, cart.CustomerId, cart.Total, cart.ItemCount,
        cart.Items.Select(i => new CartItemDto(i.Id, i.ProductVariantId, string.Empty, string.Empty, i.Quantity, i.UnitPrice, i.LineTotal)),
        cart.Status.ToString(), cart.ExpiresAt);
}

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