namespace NovaCore.Abstractions.Identity;

/// <summary>
/// Ambient information about the caller of the current request. Implemented over HttpContext in the
/// web layer; mockable to anything in tests. Keeps lower layers (auditing, authorization) HTTP-free.
/// </summary>
public interface ICurrentUser
{
    /// <summary>True when the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The caller's user id, or null for an anonymous caller.</summary>
    string? UserId { get; }

    /// <summary>The caller's user name, or null when unknown.</summary>
    string? UserName { get; }

    /// <summary>The caller's email address, or null when unknown.</summary>
    string? Email { get; }

    /// <summary>The roles the caller belongs to.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>The fine-grained permissions granted to the caller.</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>True if the caller belongs to <paramref name="role"/>.</summary>
    /// <param name="role">The role name.</param>
    bool IsInRole(string role);

    /// <summary>True if the caller carries the given fine-grained permission (or is a super-admin).</summary>
    /// <param name="permission">The permission name, e.g. <c>catalog:products:create</c>.</param>
    bool HasPermission(string permission);
}
