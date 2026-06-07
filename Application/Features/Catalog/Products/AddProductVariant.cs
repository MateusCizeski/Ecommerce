namespace Application.Features.Catalog.Products;

public record AddProductVariantCommand(Guid ProductId, string SKU, string Name, decimal Price, decimal? CompareAtPrice) : IRequest<Guid>;

public class AddProductVariantCommandValidator : AbstractValidator<AddProductVariantCommand>
{
    public AddProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(100).WithMessage("SKU inválido.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class AddProductVariantCommandHandler(IProductRepository productRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<AddProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(AddProductVariantCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.ProductId, ct)
            ?? throw new NotFoundException("Produto", cmd.ProductId);

        if (product.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var variant = product.AddVariant(cmd.SKU, cmd.Name, cmd.Price, cmd.CompareAtPrice);
        await uow.CommitAsync(ct);

        return variant.Id;
    }
}