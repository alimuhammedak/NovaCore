namespace NovaCore.Core.Abstractions.Pagination;

/// <summary>
/// A normalized paging request. Clamps page/size into sane bounds so a hostile or careless
/// caller cannot ask for page 0 or 100 000 rows.
/// </summary>
public readonly record struct PaginationRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public PaginationRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };
    }

    public int Page { get; }
    public int PageSize { get; }

    /// <summary>Number of rows to skip for the current page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
