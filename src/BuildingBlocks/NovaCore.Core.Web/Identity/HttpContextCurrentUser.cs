using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NovaCore.Core.Abstractions.Identity;

namespace NovaCore.Core.Web.Identity;

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

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? UserId =>
        Principal?.FindFirstValue(NovaClaims.Subject)
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        Principal?.FindFirstValue(NovaClaims.Name)
        ?? Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue("preferred_username");

    public string? Email =>
        Principal?.FindFirstValue(NovaClaims.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(NovaClaims.Role).Select(c => c.Value).ToArray()
        ?? [];

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(NovaClaims.Permission).Select(c => c.Value).ToArray()
        ?? [];

    public bool IsInRole(string role) =>
        Roles.Contains(role) || (Principal?.IsInRole(role) ?? false);

    public bool HasPermission(string permission) =>
        IsInRole(SuperAdminRole) || Permissions.Contains(permission);
}
