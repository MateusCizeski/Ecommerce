namespace Application.Features.Catalog.Products;

public record PublishProductCommand(Guid Id) : IRequest;

public class PublishProductCommandHandler(IProductRepository productRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<PublishProductCommand>
{
    public async Task Handle(PublishProductCommand cmd, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Produto", cmd.Id);

        if (product.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        product.Publish();
        await uow.CommitAsync(ct);
    }
}