using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Results;

namespace NovaCore.Modules.Catalog.Domain;

/// <summary>A sellable product. Aggregate root of the Catalog module.</summary>
public sealed class Product : AuditableEntity<Guid>
{
    // EF Core materialization ctor.
    private Product() { }

    public Product(string name, string? description, decimal price, int stock)
        : base(Guid.CreateVersion7())
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? description, decimal price, int stock)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
    }

    /// <summary>
    /// Takes <paramref name="quantity"/> units out of stock for a payment. Stock never goes below zero: an inactive
    /// product, or one short of stock, refuses and keeps its stock.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive: it would add stock.</exception>
    public Result RemoveStock(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (!IsActive)
            return Result.Failure(Error.Conflict("product.inactive", $"Product '{Name}' is not available."));
        if (Stock < quantity)
            return Result.Failure(Error.Conflict("product.insufficient_stock",
                $"Only {Stock} unit(s) of '{Name}' are in stock."));

        Stock -= quantity;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
