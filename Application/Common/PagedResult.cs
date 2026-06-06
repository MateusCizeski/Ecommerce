namespace Application.Common.Models;

public record PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }

    public PagedResult(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items?.ToList()?.AsReadOnly() ?? Array.Empty<T>().AsReadOnly();
        TotalCount = totalCount < 0 ? 0 : totalCount;
        CurrentPage = currentPage < 1 ? 1 : currentPage;
        PageSize = pageSize < 1 ? 1 : pageSize;
    }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;

    public static PagedResult<T> Create(IEnumerable<T> items, int total, int page, int size)
        => new(items, total, page, size);
}