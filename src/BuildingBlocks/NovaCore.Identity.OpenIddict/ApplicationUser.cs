using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// An account of the identity provider, keyed by a time-ordered <see cref="Guid"/> (version 7, friendly to clustered
/// indexes). The id is the <c>sub</c> claim of the tokens issued to the user.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Creates a user with a new version 7 id.</summary>
    public ApplicationUser() => Id = Guid.CreateVersion7();

    /// <summary>An optional name to show instead of the email address.</summary>
    public string? DisplayName { get; set; }

    /// <summary>When the account was created, from the injected <see cref="TimeProvider"/>.</summary>
    // Required, so a creator that forgets it fails to compile instead of storing 0001-01-01.
    public required DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>A role, keyed by a version 7 <see cref="Guid"/>. Its <c>permission</c> claims are the permissions it grants.</summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>Creates a role with a new version 7 id.</summary>
    public ApplicationRole() => Id = Guid.CreateVersion7();

    /// <summary>Creates a role named <paramref name="roleName"/>.</summary>
    /// <param name="roleName">The role name, e.g. <c>Administrator</c>.</param>
    public ApplicationRole(string roleName) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
    }

    /// <summary>What the role is for.</summary>
    public string? Description { get; set; }
}
