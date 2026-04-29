namespace Application.Features.Catalog.Products.DTOs;

public record ProductDetailDto(Guid Id, string Name, string Slug, string? Description, decimal BasePrice, string Status, bool IsFeatured, Guid CategoryId, string CategoryName, IEnumerable<ProductVariantDto> Variants);

public record ProductVariantDto(Guid Id, string SKU, string Name, decimal Price, decimal? CompareAtPrice, int StockQuantity, bool IsActive, IEnumerable<VariantAttributeDto> Attributes);

public record VariantAttributeDto(string Name, string Value);
