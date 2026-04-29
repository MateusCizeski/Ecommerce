using Application.Features.Catalog.Products.DTOs;
using Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Catalog.Products;

public record GetProductsQuery(int Page = 1, int PageSize = 20, string? Search = null, Guid? CategoryId = null, bool? IsFeatured = null, string? Status = null) : IRequest<PagedResult<ProductListItemDto>>;

public class GetProductsQueryHandler(IProductRepository productRepo, ITenantContext tenant) : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery q, CancellationToken ct)
    {
        var query = productRepo.Query(tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => p.Name.Contains(q.Search) || p.Slug.Contains(q.Search));
        if (q.CategoryId.HasValue) query = query.Where(p => p.CategoryId == q.CategoryId);
        if (q.IsFeatured.HasValue) query = query.Where(p => p.IsFeatured == q.IsFeatured);
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<ProductStatus>(q.Status, true, out var status))
            query = query.Where(p => p.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(p => new ProductListItemDto(p.Id, p.Name, p.Slug, p.BasePrice, p.Status.ToString(), p.IsFeatured, p.Variants.Count(v => v.IsActive), p.Category.Name))
            .ToListAsync(ct);
        return new PagedResult<ProductListItemDto>(items, total, q.Page, q.PageSize);
    }
}