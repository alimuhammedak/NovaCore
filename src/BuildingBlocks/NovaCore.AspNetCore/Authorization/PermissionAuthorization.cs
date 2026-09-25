using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
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
/// Grants access when the principal carries a matching <c>permission</c> claim, or belongs to the
/// super-admin role. Permissions are embedded in the access token as claims at login time.
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
/// Builds an authorization policy on the fly for any policy name of the form <c>perm:&lt;permission&gt;</c>,
/// so we never have to register a named policy per permission. Falls back to the default provider otherwise.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>The prefix that marks a policy name as a permission policy.</summary>
    public const string PolicyPrefix = "perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    /// <summary>Creates the provider; non-permission policies go to the default provider built from <paramref name="options"/>.</summary>
    /// <param name="options">The authorization options.</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) =>
        _fallback = new DefaultAuthorizationPolicyProvider(options);

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[PolicyPrefix.Length..]))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>Declarative form: <c>[HasPermission("catalog:products:create")]</c>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Requires <paramref name="permission"/> on the decorated class or method.</summary>
    /// <param name="permission">The permission name, e.g. <c>catalog:products:create</c>.</param>
    public HasPermissionAttribute(string permission) => Policy = PermissionPolicyProvider.PolicyPrefix + permission;
}
