namespace Application.Features.Catalog.Products.DTOs;

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    decimal BasePrice,
    string Status,
    bool IsFeatured,
    int VariantCount,
    string CategoryName);
