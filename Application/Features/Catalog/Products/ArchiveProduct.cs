using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Catalog.Products;

public record ArchiveProductCommand(Guid Id) : IRequest;

public class ArchiveProductCommandHandler(IProductRepository productRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<ArchiveProductCommand>
{
    public async Task Handle(ArchiveProductCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Product), cmd.Id);
        if (product.TenantId != tenant.TenantId) throw new TenantAccessException();
        product.Archive();
        await uow.CommitAsync(ct);
    }
}
