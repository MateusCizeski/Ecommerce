using Application.Exceptions;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application.Features.Tenancy.Tenants;

public record CreateTenantCommand(string Name, string Subdomain, string Email) : IRequest<Guid>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subdomain).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class CreateTenantCommandHandler(ITenantRepository tenantRepo, IUnitOfWork uow) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand cmd, CancellationToken ct)
    {
        if (await tenantRepo.SubdomainExistsAsync(cmd.Subdomain, ct))
            throw new ConflictException($"Subdomain '{cmd.Subdomain}' is already taken.");
        var tenant = Tenant.Create(cmd.Name, cmd.Subdomain, cmd.Email);
        await tenantRepo.AddAsync(tenant, ct);
        await uow.CommitAsync(ct);
        return tenant.Id;
    }
}