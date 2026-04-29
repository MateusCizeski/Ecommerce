using Application.Exceptions;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application;

public record CreateCategoryCommand(string Name, string Slug, string? Description, Guid? ParentCategoryId, int SortOrder = 0) : IRequest<Guid>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryCommandHandler(ICategoryRepository categoryRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        if (await categoryRepo.SlugExistsAsync(tenant.TenantId, cmd.Slug, ct))
            throw new ConflictException($"Slug '{cmd.Slug}' is already in use.");
        if (cmd.ParentCategoryId.HasValue)
        {
            var parent = await categoryRepo.GetByIdAsync(cmd.ParentCategoryId.Value, ct)
                ?? throw new NotFoundException(nameof(Category), cmd.ParentCategoryId.Value);
            if (parent.TenantId != tenant.TenantId) throw new TenantAccessException();
        }
        var category = Category.Create(tenant.TenantId, cmd.Name, cmd.Slug, cmd.Description, cmd.ParentCategoryId, cmd.SortOrder);
        await categoryRepo.AddAsync(category, ct);
        await uow.CommitAsync(ct);
        return category.Id;
    }
}

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

public record CategoryListItemDto(Guid Id, string Name, string Slug, bool IsActive, int SortOrder, int ProductCount, int SubCategoryCount, Guid? ParentCategoryId, string? ParentCategoryName);

public record CategoryDetailDto(Guid Id, string Name, string Slug, string? Description, bool IsActive, int SortOrder, Guid? ParentCategoryId, string? ParentCategoryName, IEnumerable<CategoryListItemDto> SubCategories);

public record GetCategoriesQuery(bool? IsActive = null, Guid? ParentId = null, int Page = 1, int PageSize = 50) : IRequest<PagedResult<CategoryListItemDto>>;

public class GetCategoriesQueryHandler(ICategoryRepository categoryRepo, ITenantContext tenant) : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryListItemDto>>
{
    public async Task<PagedResult<CategoryListItemDto>> Handle(GetCategoriesQuery q, CancellationToken ct)
    {
        var query = categoryRepo.Query(tenant.TenantId);
        if (q.IsActive.HasValue) query = query.Where(c => c.IsActive == q.IsActive);
        query = q.ParentId.HasValue ? query.Where(c => c.ParentCategoryId == q.ParentId) : query.Where(c => c.ParentCategoryId == null);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(c => new CategoryListItemDto(c.Id, c.Name, c.Slug, c.IsActive, c.SortOrder, c.Products.Count, c.SubCategories.Count, c.ParentCategoryId, c.ParentCategory != null ? c.ParentCategory.Name : null))
            .ToListAsync(ct);
        return new PagedResult<CategoryListItemDto>(items, total, q.Page, q.PageSize);
    }
}

public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDetailDto>;

public class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepo, ITenantContext tenant) : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(GetCategoryByIdQuery q, CancellationToken ct)
    {
        var c = await categoryRepo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException(nameof(Category), q.Id);
        if (c.TenantId != tenant.TenantId) throw new TenantAccessException();
        return new CategoryDetailDto(c.Id, c.Name, c.Slug, c.Description, c.IsActive, c.SortOrder, c.ParentCategoryId, c.ParentCategory?.Name,
            c.SubCategories.Select(s => new CategoryListItemDto(s.Id, s.Name, s.Slug, s.IsActive, s.SortOrder, s.Products.Count, s.SubCategories.Count, s.ParentCategoryId, c.Name)));
    }
}