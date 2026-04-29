using Application.Exceptions;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application.Features.Catalog.Products;

public record UpdateProductCommand(Guid Id, string Name, string Slug, decimal BasePrice, Guid CategoryId, string? Description, bool IsFeatured) : IRequest;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateProductCommandHandler(IProductRepository productRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Product), cmd.Id);
        if (product.TenantId != tenant.TenantId) throw new TenantAccessException();
        if (product.Slug != cmd.Slug && await productRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"Slug '{cmd.Slug}' is already in use.");
        product.Update(cmd.Name, cmd.Slug, cmd.BasePrice, cmd.CategoryId, cmd.Description, cmd.IsFeatured);
        await uow.CommitAsync(ct);
    }
}