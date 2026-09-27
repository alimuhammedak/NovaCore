using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;

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
            .WithName("AdminAssignRole").WithSummary("Grant a role to a user")
            .RequirePermission(IdentityPermissions.UsersManage);

        admin.MapDelete("/users/{userId:guid}/roles/{role}", RemoveRole)
            .WithName("AdminRemoveRole").WithSummary("Revoke a role from a user")
            .RequirePermission(IdentityPermissions.UsersManage);
    }

    internal sealed record UserSummary(Guid Id, string? Email, string? UserName, bool EmailConfirmed, bool TwoFactorEnabled, bool LockedOut);

    private static async Task<IResult> ListUsers(
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

        return Results.Ok(new PagedList<UserSummary>(items, pagination.Page, pagination.PageSize, total));
    }

    private static async Task<IResult> ListRoles(RoleManager<ApplicationRole> roles, CancellationToken cancellationToken)
    {
        var list = await roles.Roles
            .AsNoTracking()
            .Select(role => new { role.Id, role.Name, role.Description })
            .ToListAsync(cancellationToken);

        return Results.Ok(list);
    }

    private static IResult ListPermissions(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        var permissions = providers
            .SelectMany(provider => provider.GetPermissions())
            .DistinctBy(permission => permission.Name)
            .GroupBy(permission => permission.Group)
            .Select(group => new { group = group.Key, permissions = group.OrderBy(permission => permission.Name) });

        return Results.Ok(permissions);
    }

    private static async Task<IResult> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ICurrentUser currentUser,
        SecurityAuditLogger audit)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound(new { message = "User not found." });

        if (!await roles.RoleExistsAsync(request.Role))
            return Results.NotFound(new { message = "Role not found." });

        if (!await users.IsInRoleAsync(user, request.Role))
        {
            var result = await users.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = result.Errors.Select(error => error.Description).ToArray()
                });
            }

            // The permission cache picks the change up within Cache:PermissionExpiration.
            audit.RoleAssigned(user.Id, request.Role, currentUser.UserId);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveRole(
        Guid userId,
        string role,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        SecurityAuditLogger audit)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound(new { message = "User not found." });

        if (await users.IsInRoleAsync(user, role))
        {
            await users.RemoveFromRoleAsync(user, role);
            audit.RoleRemoved(user.Id, role, currentUser.UserId);
        }

        return Results.NoContent();
    }
}
