using Application.Features.Catalog.Products.DTOs;

namespace Application.Features.Catalog.Products;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsFeatured = null,
    string? Status = null) : IRequest<PagedResult<ProductListItemDto>>;

public class GetProductsQueryHandler(IProductRepository productRepo, ITenantContext tenant)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery q, CancellationToken ct)
    {
        var query = productRepo.Query(tenant.TenantId);

        // Filtro de texto por similaridade
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var searchLower = q.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) || p.Slug.ToLower().Contains(searchLower));
        }

        if (q.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == q.CategoryId);

        if (q.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == q.IsFeatured);

        // Conversão defensiva de string para Enum
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<ProductStatus>(q.Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        // Projeção LINQ e delegação da paginação assíncrona desacoplada do EF Core tradicional
        var projectedQuery = query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.BasePrice,
                p.Status.ToString(),
                p.IsFeatured,
                p.Variants.Count(v => v.IsActive),
                p.Category.Name
            ));

        return await projectedQuery.ToPagedResultAsync(q.Page, q.PageSize, ct);
    }
}