using Application.Features.Catalog.Categories.DTOs;
using Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Catalog.Categories;

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