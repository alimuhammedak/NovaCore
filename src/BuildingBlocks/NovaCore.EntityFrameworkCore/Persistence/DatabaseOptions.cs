namespace NovaCore.EntityFrameworkCore.Persistence;

/// <summary>Bound from the <c>Database</c> section.</summary>
internal sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply every module's pending EF Core migrations on startup. Off by default (R-003).</summary>
    public bool ApplyMigrations { get; set; }
}
