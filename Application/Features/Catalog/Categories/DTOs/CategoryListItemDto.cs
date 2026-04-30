namespace Application.Features.Catalog.Categories.DTOs;

public record CategoryListItemDto(Guid Id, string Name, string Slug, bool IsActive, int SortOrder, int ProductCount, int SubCategoryCount, Guid? ParentCategoryId, string? ParentCategoryName);

public record CategoryDetailDto(Guid Id, string Name, string Slug, string? Description, bool IsActive, int SortOrder, Guid? ParentCategoryId, string? ParentCategoryName, IEnumerable<CategoryListItemDto> SubCategories);