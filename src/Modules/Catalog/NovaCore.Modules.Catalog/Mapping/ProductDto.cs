namespace NovaCore.Modules.Catalog.Mapping;

/// <summary>The product representation returned over the wire.</summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
