namespace NovaCore.Modules.Identity.Options;

/// <summary>Bound from <c>Identity:Seed</c>. Drives startup seeding of roles, permissions and the admin user.</summary>
public sealed class IdentitySeedOptions
{
    public const string SectionName = "Identity:Seed";

    /// <summary>Run seeding on startup. Requires the database to already be migrated.</summary>
    public bool SeedOnStartup { get; set; }

    /// <summary>Optional initial admin. Leave the password unset (use user-secrets) to skip admin creation.</summary>
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
}
