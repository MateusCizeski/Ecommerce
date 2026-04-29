using MediatR;
using FluentValidation;
using Ecommerce.Domain;
using Domain.Interfaces;
using Application.Exceptions;

namespace Application.Features.Catalog.Products;

public record CreateProductCommand(string Name, string Slug, decimal BasePrice, Guid CategoryId, string? Description, bool IsFeatured) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers and hyphens.");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class CreateProductCommandHandler(IProductRepository productRepo, ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(cmd.CategoryId, ct)
            ?? throw new NotFoundException(nameof(Category), cmd.CategoryId);
        if (category.TenantId != tenant.TenantId) throw new TenantAccessException();
        if (await productRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"Slug '{cmd.Slug}' is already in use.");

        var product = Product.Create(tenant.TenantId, cmd.CategoryId, cmd.Name, cmd.Slug, cmd.BasePrice, cmd.Description);
        await productRepo.AddAsync(product, ct);
        await uow.CommitAsync(ct);
        return product.Id;
    }
}