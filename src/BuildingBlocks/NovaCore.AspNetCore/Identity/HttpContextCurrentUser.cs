using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NovaCore.Abstractions.Identity;

namespace NovaCore.AspNetCore.Identity;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the request's authenticated principal. Reads the raw JWT
/// claims NovaCore issues (<see cref="NovaClaims"/>), with the classic ClaimTypes URIs as a fallback,
/// so audit and authorization stay HTTP-agnostic upstream.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>Roles whose holders implicitly satisfy every permission check.</summary>
    public const string SuperAdminRole = "Administrator";

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    /// <remarks>Reads <c>sub</c>, falling back to <see cref="ClaimTypes.NameIdentifier"/>.</remarks>
    public string? UserId =>
        Principal?.FindFirstValue(NovaClaims.Subject)
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc />
    /// <remarks>Reads <c>name</c>, then <see cref="ClaimTypes.Name"/>, then <c>preferred_username</c>.</remarks>
    public string? UserName =>
        Principal?.FindFirstValue(NovaClaims.Name)
        ?? Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue("preferred_username");

    /// <inheritdoc />
    /// <remarks>Reads <c>email</c>, falling back to <see cref="ClaimTypes.Email"/>.</remarks>
    public string? Email =>
        Principal?.FindFirstValue(NovaClaims.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(NovaClaims.Role).Select(c => c.Value).ToArray()
        ?? [];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(NovaClaims.Permission).Select(c => c.Value).ToArray()
        ?? [];

    /// <inheritdoc />
    public bool IsInRole(string role) =>
        Roles.Contains(role) || (Principal?.IsInRole(role) ?? false);

    /// <inheritdoc />
    /// <remarks>Holders of <see cref="SuperAdminRole"/> have every permission.</remarks>
    public bool HasPermission(string permission) =>
        IsInRole(SuperAdminRole) || Permissions.Contains(permission);
}
