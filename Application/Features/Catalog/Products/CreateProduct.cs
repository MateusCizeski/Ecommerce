namespace Application.Features.Catalog.Products;

public record CreateProductCommand(string Name, string Slug, decimal BasePrice, Guid CategoryId, string? Description, bool IsFeatured) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("O nome do produto é obrigatório.");
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$").WithMessage("O slug deve conter apenas letras minúsculas, números e hifens.");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).WithMessage("O preço base não pode ser negativo.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("A categoria é obrigatória.");
    }
}

public class CreateProductCommandHandler(IProductRepository productRepo, ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(cmd.CategoryId, ct)
            ?? throw new NotFoundException("Categoria", cmd.CategoryId);

        if (category.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        if (await productRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"O slug '{cmd.Slug}' já está em uso neste tenant.");

        var product = Product.Create(tenant.TenantId, cmd.CategoryId, cmd.Name, cmd.Slug, cmd.BasePrice, cmd.Description);

        await productRepo.AddAsync(product, ct);
        await uow.CommitAsync(ct);

        return product.Id;
    }
}