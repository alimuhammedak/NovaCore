namespace NovaCore.Abstractions.Pagination;

/// <summary>
/// A normalized paging request. Clamps page/size into sane bounds so a hostile or careless
/// caller cannot ask for page 0 or 100 000 rows.
/// </summary>
public readonly record struct PaginationRequest
{
    /// <summary>The page size used when none (or a value below 1) is requested.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest page size a caller can get; bigger requests are clamped to this.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Creates a request, clamping out-of-range values instead of rejecting them.</summary>
    /// <param name="page">The 1-based page number; values below 1 become 1.</param>
    /// <param name="pageSize">The page size; values below 1 become <see cref="DefaultPageSize"/>, values above
    /// <see cref="MaxPageSize"/> become <see cref="MaxPageSize"/>.</param>
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

    /// <summary>The 1-based page number, as clamped by the constructor.</summary>
    public int Page { get; }

    /// <summary>The page size, as clamped by the constructor.</summary>
    public int PageSize { get; }

    /// <summary>Number of rows to skip for the current page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
