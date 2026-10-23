using CsCheck;
using NovaCore.Abstractions.Pagination;
using Xunit;

namespace NovaCore.Domain.Tests;

/// <summary>
/// Pagination arithmetic over every input the query string can carry (CsCheck): a request is always a valid page, and a
/// page's numbers agree with the total count.
/// </summary>
public sealed class PaginationPropertyTests
{
    [Fact]
    public void Any_page_and_size_become_a_valid_request_whose_offset_skips_the_pages_before_it()
    {
        // Any int, with the values around the bounds often enough to land on them.
        var pages = Gen.OneOf(Gen.Int, Gen.Int[-2, 3]);
        var sizes = Gen.OneOf(Gen.Int, Gen.Int[-2, 3], Gen.Int[PaginationRequest.MaxPageSize - 2, PaginationRequest.MaxPageSize + 2]);

        Gen.Select(pages, sizes).Sample((page, pageSize) =>
        {
            var request = new PaginationRequest(page, pageSize);

            Assert.Equal(page < 1 ? 1 : page, request.Page);
            Assert.Equal(pageSize < 1 ? PaginationRequest.DefaultPageSize : Math.Min(pageSize, PaginationRequest.MaxPageSize),
                request.PageSize);
            Assert.Equal(Math.Min((request.Page - 1L) * request.PageSize, int.MaxValue), request.Skip);
        });
    }

    [Fact]
    public void A_pages_counts_agree_with_the_total()
    {
        // Small numbers, so the first and the last page come up often.
        Gen.Select(Gen.Int[1, 12], Gen.Int[1, 10], Gen.Long[0, 100])
            .Sample((page, pageSize, totalCount) =>
            {
                var list = new PagedList<int>([], page, pageSize, totalCount);

                Assert.True((long)list.TotalPages * pageSize >= totalCount);
                Assert.True(totalCount == 0 ? list.TotalPages == 0 : (list.TotalPages - 1L) * pageSize < totalCount);
                Assert.Equal(page < list.TotalPages, list.HasNextPage);
                Assert.Equal(page > 1, list.HasPreviousPage);
            });
    }
}
