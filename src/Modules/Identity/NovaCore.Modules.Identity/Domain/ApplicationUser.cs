using Microsoft.AspNetCore.Identity;

namespace NovaCore.Modules.Identity.Domain;

/// <summary>Application user, keyed by <see cref="Guid"/> (time-ordered v7 → friendly to SQL Server clustering).</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser() => Id = Guid.CreateVersion7();

    public string? DisplayName { get; set; }
    // Required: the creator sets it from the injected TimeProvider, and forgetting it is a compile error instead of a
    // silent 0001-01-01.
    public required DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Application role, keyed by <see cref="Guid"/>. Permissions are attached as role claims.</summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() => Id = Guid.CreateVersion7();

    public ApplicationRole(string roleName) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
    }

    public string? Description { get; set; }
}
