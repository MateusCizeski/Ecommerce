namespace Application.Features.Catalog.Categories;

public record CreateCategoryCommand(string Name, string Slug, string? Description, Guid? ParentCategoryId, int SortOrder = 0) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Nome é obrigatório e deve ter no máximo 200 caracteres.");
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$").WithMessage("Slug inválido. Use apenas letras minúsculas, números e hifens.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryCommandHandler(ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        if (await categoryRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"O slug '{cmd.Slug}' já está em uso neste tenant.");

        if (cmd.ParentCategoryId.HasValue)
        {
            var parent = await categoryRepo.GetByIdAsync(cmd.ParentCategoryId.Value, ct)
                ?? throw new NotFoundException("Categoria Pai", cmd.ParentCategoryId.Value);

            if (parent.TenantId != tenant.TenantId)
                throw new ForbiddenException("Você não tem permissão para acessar esta categoria pai.");
        }

        var category = Category.Create(tenant.TenantId, cmd.Name, cmd.Slug, cmd.Description, cmd.ParentCategoryId, cmd.SortOrder);

        await categoryRepo.AddAsync(category, ct);
        await uow.CommitAsync(ct);

        return category.Id;
    }
}