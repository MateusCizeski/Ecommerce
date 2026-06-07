namespace Application.Features.Catalog.Products;

public record ArchiveProductCommand(Guid Id) : IRequest;

public class ArchiveProductCommandHandler(IProductRepository productRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<ArchiveProductCommand>
{
    public async Task Handle(ArchiveProductCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Produto", cmd.Id);

        if (product.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        product.Archive();
        await uow.CommitAsync(ct);
    }
}