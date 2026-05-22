namespace BoutiqueInventory.Application.Common;

/// <summary>
/// Standard envelope for paginated query results.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
