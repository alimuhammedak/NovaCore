using CsCheck;
using NovaCore.Abstractions.Pagination;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// Pagination arithmetic over every input the query string can carry (CsCheck): a request is always a valid page, and a
/// page's numbers agree with the total count.
/// </summary>
public sealed class PaginationPropertyTests
{
    [Fact]
    public void Any_page_and_size_become_a_valid_request_whose_offset_skips_the_pages_before_it()
    {
        Gen.Select(Gen.Int, Gen.Int).Sample((page, pageSize) =>
        {
            var request = new PaginationRequest(page, pageSize);

            Assert.InRange(request.Page, 1, int.MaxValue);
            Assert.InRange(request.PageSize, 1, PaginationRequest.MaxPageSize);
            Assert.Equal(Math.Min((request.Page - 1L) * request.PageSize, int.MaxValue), request.Skip);
        });
    }

    [Fact]
    public void A_pages_counts_agree_with_the_total()
    {
        Gen.Select(Gen.Int[1, 10_000], Gen.Int[1, PaginationRequest.MaxPageSize], Gen.Long[0, 1_000_000_000])
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
