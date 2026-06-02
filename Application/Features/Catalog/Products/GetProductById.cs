using Application.Features.Catalog.Products.DTOs;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Catalog.Products;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailDto>;

public class GetProductByIdQueryHandler(IProductRepository productRepo, ITenantContext tenant) : IRequestHandler<GetProductByIdQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(GetProductByIdQuery q, CancellationToken ct)
    {
        var p = await productRepo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("Product", q.Id);
        if (p.TenantId != tenant.TenantId) throw new TenantAccessException();
        return new ProductDetailDto(p.Id, p.Name, p.Slug, p.Description, p.BasePrice, p.Status.ToString(), p.IsFeatured, p.CategoryId, p.Category.Name,
            p.Variants.Select(v => new ProductVariantDto(v.Id, v.SKU, v.Name, v.Price, v.CompareAtPrice, v.StockQuantity, v.IsActive,
                v.Attributes.Select(a => new VariantAttributeDto(a.AttributeName, a.AttributeValue)))));
    }
}
