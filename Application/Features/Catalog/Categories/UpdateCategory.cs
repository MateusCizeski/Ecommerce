using MediatR;
using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Application.Exceptions;

namespace Application.Features.Catalog.Categories;

public record UpdateCategoryCommand(Guid Id, string Name, string Slug, string? Description, int SortOrder) : IRequest;

public class UpdateCategoryCommandHandler(ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Category), cmd.Id);
        if (category.TenantId != tenant.TenantId) throw new TenantAccessException();
        if (category.Slug != cmd.Slug && await categoryRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"Slug '{cmd.Slug}' is already in use.");
        category.Update(cmd.Name, cmd.Slug, cmd.Description, cmd.SortOrder);
        await uow.CommitAsync(ct);
    }
}
