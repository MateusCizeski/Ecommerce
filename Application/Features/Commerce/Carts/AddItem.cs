using Application.Features.Commerce.Cart.DTOs;

namespace Application.Features.Commerce.Cart;

public record AddCartItemCommand(Guid CartId, Guid VariantId, int Quantity) : IRequest<CartDto>;

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty().WithMessage("O ID do carrinho é obrigatório.");
        RuleFor(x => x.VariantId).NotEmpty().WithMessage("O ID da variante do produto é obrigatório.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}

public class AddCartItemCommandHandler(
    ICartRepository cartRepo,
    IProductVariantRepository variantRepo,
    IUnitOfWork uow,
    ITenantContext tenant) : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand cmd, CancellationToken ct)
    {
        var cart = await cartRepo.GetByIdAsync(cmd.CartId, ct)
            ?? throw new NotFoundException("Carrinho", cmd.CartId);

        if (cart.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var variant = await variantRepo.GetByIdAsync(cmd.VariantId, ct)
            ?? throw new NotFoundException("Variante do Produto", cmd.VariantId);

        cart.AddItem(variant, cmd.Quantity);

        await uow.CommitAsync(ct);

        return cart.ToDto();
    }
}
