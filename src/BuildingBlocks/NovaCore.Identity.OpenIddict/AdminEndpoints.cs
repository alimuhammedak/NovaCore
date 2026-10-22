using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>User and role administration under <c>/api/v1/admin</c>, each endpoint guarded by a permission.</summary>
internal static class AdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin").WithTags("Admin");

        admin.MapGet("/users", ListUsers)
            .WithName("AdminListUsers").WithSummary("List users (paged)")
            .RequirePermission(IdentityPermissions.UsersRead);

        admin.MapGet("/roles", ListRoles)
            .WithName("AdminListRoles").WithSummary("List roles")
            .RequirePermission(IdentityPermissions.RolesRead);

        admin.MapGet("/permissions", ListPermissions)
            .WithName("AdminListPermissions").WithSummary("List all permissions declared by modules")
            .RequirePermission(IdentityPermissions.RolesRead);

        admin.MapPost("/users/{userId:guid}/roles", AssignRole)
            .WithName("AdminAssignRole").WithSummary("Grant a role to a user (Administrator also needs identity:roles:manage; ends the user's sessions)")
            .RequirePermission(IdentityPermissions.UsersManage);

        admin.MapDelete("/users/{userId:guid}/roles/{role}", RemoveRole)
            .WithName("AdminRemoveRole").WithSummary("Revoke a role from a user (Administrator also needs identity:roles:manage; the last administrator keeps it)")
            .RequirePermission(IdentityPermissions.UsersManage);
    }

    internal sealed record UserSummary(Guid Id, string? Email, string? UserName, bool EmailConfirmed, bool TwoFactorEnabled, bool LockedOut);

    internal sealed record RoleSummary(Guid Id, string? Name, string? Description);

    internal sealed record PermissionGroup(string Group, IReadOnlyList<PermissionDefinition> Permissions);

    private static async Task<Ok<PagedList<UserSummary>>> ListUsers(
        UserManager<ApplicationUser> users,
        TimeProvider timeProvider,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = users.Users.AsNoTracking().OrderBy(user => user.Email);
        var total = await query.LongCountAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var items = await query
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(user => new UserSummary(
                user.Id, user.Email, user.UserName, user.EmailConfirmed, user.TwoFactorEnabled,
                user.LockoutEnd != null && user.LockoutEnd > now))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedList<UserSummary>(items, pagination.Page, pagination.PageSize, total));
    }

    private static async Task<Ok<List<RoleSummary>>> ListRoles(RoleManager<ApplicationRole> roles, CancellationToken cancellationToken)
    {
        var list = await roles.Roles
            .AsNoTracking()
            .Select(role => new RoleSummary(role.Id, role.Name, role.Description))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(list);
    }

    private static Ok<PermissionGroup[]> ListPermissions(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        var permissions = providers
            .SelectMany(provider => provider.GetPermissions())
            .DistinctBy(permission => permission.Name)
            .GroupBy(permission => permission.Group)
            .Select(group => new PermissionGroup(group.Key, [.. group.OrderBy(permission => permission.Name)]))
            .ToArray();

        return TypedResults.Ok(permissions);
    }

    private static async Task<Results<NoContent, NotFound<AccountEndpoints.MessageResponse>, ValidationProblem, ForbidHttpResult>> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ICurrentUser currentUser,
        HybridCache cache,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        if (IsAdministrator(roles, request.Role) && !currentUser.HasPermission(IdentityPermissions.RolesManage))
            return TypedResults.Forbid();

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return UserNotFound();

        if (!await roles.RoleExistsAsync(request.Role))
            return TypedResults.NotFound(new AccountEndpoints.MessageResponse("Role not found."));

        if (!await users.IsInRoleAsync(user, request.Role))
        {
            var result = await users.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = result.Errors.Select(error => error.Description).ToArray()
                });
            }

            await RoleChangedAsync(user, cache, tokens, authorizations, cancellationToken);
            audit.RoleAssigned(user.Id, request.Role, currentUser.UserId);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<AccountEndpoints.MessageResponse>, Conflict<AccountEndpoints.MessageResponse>, ForbidHttpResult>> RemoveRole(
        Guid userId,
        string role,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ICurrentUser currentUser,
        HybridCache cache,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        var administrator = IsAdministrator(roles, role);
        if (administrator && !currentUser.HasPermission(IdentityPermissions.RolesManage))
            return TypedResults.Forbid();

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return UserNotFound();

        if (await users.IsInRoleAsync(user, role))
        {
            // Without an administrator nobody could grant the role again, short of editing the database. Only a member with
            // two-factor authentication has the role in effect (IdentityPermissionResolver), so one of them must remain.
            if (administrator && !(await users.GetUsersInRoleAsync(IdentityRoles.Administrator))
                    .Any(member => member.Id != user.Id && member.TwoFactorEnabled))
                return TypedResults.Conflict(new AccountEndpoints.MessageResponse("The last administrator keeps the role."));

            await users.RemoveFromRoleAsync(user, role);
            await RoleChangedAsync(user, cache, tokens, authorizations, cancellationToken);
            audit.RoleRemoved(user.Id, role, currentUser.UserId);
        }

        return TypedResults.NoContent();
    }

    // The Administrator role holds every permission, identity:roles:manage included, so identity:users:manage alone
    // mustn't hand it out or take it away: its holder could make themselves an administrator. Role names are compared
    // the way the store finds them (normalized), so "administrator" is the same role.
    private static bool IsAdministrator(RoleManager<ApplicationRole> roles, string role) =>
        string.Equals(roles.NormalizeKey(role), roles.NormalizeKey(IdentityRoles.Administrator), StringComparison.Ordinal);

    // The user's permissions are resolved again on their next request, not when the cached ones expire; the refresh
    // tokens and authorizations issued under the old roles end (the access tokens run out within 15 minutes).
    private static async Task RoleChangedAsync(
        ApplicationUser user,
        HybridCache cache,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        CancellationToken cancellationToken)
    {
        await cache.RemovePermissionsAsync(user.Id.ToString(), cancellationToken);
        await AccountEndpoints.RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
    }

    private static NotFound<AccountEndpoints.MessageResponse> UserNotFound() =>
        TypedResults.NotFound(new AccountEndpoints.MessageResponse("User not found."));
}
