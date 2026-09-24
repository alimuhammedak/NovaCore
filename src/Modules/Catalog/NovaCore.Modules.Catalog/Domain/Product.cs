using NovaCore.Core.Abstractions.Entities;

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

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
