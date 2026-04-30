using Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Catalog.Categories;

public record DeactivateCategoryCommand(Guid Id) : IRequest;

public class DeactivateCategoryCommandHandler(ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<DeactivateCategoryCommand>
{
    public async Task Handle(DeactivateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Category), cmd.Id);
        if (category.TenantId != tenant.TenantId) throw new TenantAccessException();
        category.Deactivate();
        await uow.CommitAsync(ct);
    }
}