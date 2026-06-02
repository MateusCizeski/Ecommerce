using MediatR;
using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;

namespace Application.Features.Tenancy.Tenants;

public record UpdateTenantCommand(Guid Id, string Name, string Email) : IRequest;

public class UpdateTenantCommandHandler(ITenantRepository tenantRepo, IUnitOfWork uow) : IRequestHandler<UpdateTenantCommand>
{
    public async Task Handle(UpdateTenantCommand cmd, CancellationToken ct)
    {
        var tenant = await tenantRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Tenant), cmd.Id);
        tenant.Update(cmd.Name, cmd.Email);
        await uow.CommitAsync(ct);
    }
}
