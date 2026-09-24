namespace NovaCore.Core.Abstractions.Entities;

/// <summary>
/// Entities that should be stamped with creation/modification audit data automatically.
/// The stamping is done centrally by an EF Core interceptor (see Core.Infrastructure).
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
}
