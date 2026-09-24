namespace NovaCore.Core.Abstractions.Identity;

/// <summary>
/// Ambient information about the caller of the current request. Implemented over HttpContext in the
/// web layer; mockable to anything in tests. Keeps lower layers (auditing, authorization) HTTP-free.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsInRole(string role);

    /// <summary>True if the caller carries the given fine-grained permission (or is a super-admin).</summary>
    bool HasPermission(string permission);
}
