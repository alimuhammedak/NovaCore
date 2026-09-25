namespace NovaCore.Abstractions.Entities;

/// <summary>
/// Entities that should be stamped with creation/modification audit data automatically.
/// The stamping is done centrally by an EF Core interceptor (see Core.Infrastructure).
/// </summary>
public interface IAuditableEntity
{
    /// <summary>When the entity was first saved, in UTC.</summary>
    DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Who first saved the entity.</summary>
    string? CreatedBy { get; set; }

    /// <summary>When the entity was last updated, in UTC; null until the first update.</summary>
    DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <summary>Who last updated the entity; null until the first update.</summary>
    string? ModifiedBy { get; set; }
}
