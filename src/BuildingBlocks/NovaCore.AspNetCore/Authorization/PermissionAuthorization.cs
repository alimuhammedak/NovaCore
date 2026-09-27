using Microsoft.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>Requires the caller to hold a specific fine-grained permission.</summary>
/// <param name="permission">The permission name, e.g. <c>catalog:products:create</c>.</param>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>The permission the caller must hold.</summary>
    public string Permission { get; } = permission;
}

/// <summary>
/// Grants access when the principal carries a matching <c>permission</c> claim, or belongs to the super-admin role.
/// The permission claims are the ones resolved on the server (<see cref="IPermissionResolver"/>), never the token's own.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission =
            context.User.IsInRole(HttpContextCurrentUser.SuperAdminRole) ||
            context.User.HasClaim(NovaClaims.Permission, requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requires an authenticated caller that holds <see cref="Permission"/> (or the super-admin role):
/// <c>[RequirePermission("catalog:products:create")]</c> on a handler, or <c>.RequirePermission(...)</c> on an endpoint.
/// The attribute carries its own <see cref="PermissionRequirement"/> (<see cref="IAuthorizationRequirementData"/>), so
/// no named policy is registered per permission. .NET 10 enforces it on Minimal API and other routed endpoints, not on
/// MVC controllers, SignalR hubs or Blazor components.
/// </summary>
/// <param name="permission">The permission name, e.g. <c>catalog:products:create</c>.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>The permission the caller must hold.</summary>
    public string Permission { get; } = permission;

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(Permission)];
}
