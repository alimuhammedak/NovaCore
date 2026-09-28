using NovaCore.Modules.Catalog.Domain;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// PLAN F4b.2 (D-035): a payment's stock comes out of its product through <see cref="Product.RemoveStock"/>. Stock never
/// goes below zero: a product that is short of stock or inactive refuses and keeps its stock.
/// </summary>
public class ProductStockTests
{
    [Fact]
    public void Removing_stock_takes_it_from_the_product()
    {
        var product = new Product("Widget", null, 10m, stock: 5);

        var result = product.RemoveStock(2);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, product.Stock);
    }

    [Fact]
    public void The_last_units_can_be_taken()
    {
        var product = new Product("Widget", null, 10m, stock: 2);

        var result = product.RemoveStock(2);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public void More_than_the_stock_is_refused_and_nothing_is_taken()
    {
        var product = new Product("Widget", null, 10m, stock: 1);

        var result = product.RemoveStock(2);

        Assert.Equal("product.insufficient_stock", result.Error.Code);
        Assert.Equal(1, product.Stock);
    }

    [Fact]
    public void An_inactive_product_gives_no_stock()
    {
        var product = new Product("Widget", null, 10m, stock: 5);
        product.Deactivate();

        var result = product.RemoveStock(1);

        Assert.Equal("product.inactive", result.Error.Code);
        Assert.Equal(5, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Removing_no_units_or_fewer_is_a_bug(int quantity)
    {
        var product = new Product("Widget", null, 10m, stock: 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.RemoveStock(quantity));
        Assert.Equal(5, product.Stock);
    }
}
