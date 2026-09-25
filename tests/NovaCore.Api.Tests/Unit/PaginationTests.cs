using System.Globalization;
using NovaCore.Abstractions.Pagination;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_is_clamped_to_at_least_one(int requested, int expected)
    {
        Assert.Equal(expected, new PaginationRequest(requested).Page);
    }

    [Theory]
    [InlineData(0, PaginationRequest.DefaultPageSize)]
    [InlineData(5000, PaginationRequest.MaxPageSize)]
    [InlineData(25, 25)]
    public void PageSize_is_clamped_into_bounds(int requested, int expected)
    {
        Assert.Equal(expected, new PaginationRequest(1, requested).PageSize);
    }

    [Fact]
    public void Skip_is_computed_from_page_and_size()
    {
        Assert.Equal(40, new PaginationRequest(3, 20).Skip);
    }

    [Fact]
    public void PagedList_computes_navigation_metadata()
    {
        var page = new PagedList<int>([1, 2, 3], page: 2, pageSize: 3, totalCount: 10);

        Assert.Equal(4, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void PagedList_map_preserves_metadata()
    {
        var page = new PagedList<int>([1, 2], page: 1, pageSize: 2, totalCount: 2);

        var mapped = page.Map(x => x.ToString(CultureInfo.InvariantCulture));

        Assert.Equal(["1", "2"], mapped.Items);
        Assert.Equal(page.TotalCount, mapped.TotalCount);
    }
}
