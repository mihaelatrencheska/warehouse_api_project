namespace BoutiqueInventory.Application.Common;

/// <summary>Pagination defaults shared across the application.</summary>
public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Coerce caller-supplied paging values into safe bounds.</summary>
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var p = page is null or < 1 ? DefaultPage : page.Value;
        var s = pageSize switch
        {
            null => DefaultPageSize,
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };
        return (p, s);
    }
}
