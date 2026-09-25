using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;
using NovaCore.FluentValidation;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity.Features;

/// <summary>Administrative user/role/permission management, each guarded by a fine-grained permission.</summary>
public static class AdminEndpoints
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/users", ListUsers)
            .WithName("AdminListUsers").WithSummary("List users (paged)")
            .RequirePermission(IdentityPermissions.UsersRead);

        group.MapGet("/roles", ListRoles)
            .WithName("AdminListRoles").WithSummary("List roles")
            .RequirePermission(IdentityPermissions.RolesRead);

        group.MapGet("/permissions", ListPermissions)
            .WithName("AdminListPermissions").WithSummary("List all permissions declared by modules")
            .RequirePermission(IdentityPermissions.RolesRead);

        group.MapPost("/users/{userId:guid}/roles", AssignRole)
            .WithName("AdminAssignRole").WithSummary("Grant a role to a user")
            .AddEndpointFilter<ValidationFilter<AssignRoleRequest>>()
            .RequirePermission(IdentityPermissions.UsersManage);

        group.MapDelete("/users/{userId:guid}/roles/{role}", RemoveRole)
            .WithName("AdminRemoveRole").WithSummary("Revoke a role from a user")
            .RequirePermission(IdentityPermissions.UsersManage);
    }

    public sealed record AssignRoleRequest(string Role);
    public sealed record UserSummary(Guid Id, string? Email, string? UserName, bool EmailConfirmed, bool TwoFactorEnabled, bool LockedOut);

    public sealed class AssignRoleValidator : AbstractValidator<AssignRoleRequest>
    {
        public AssignRoleValidator() => RuleFor(x => x.Role).NotEmpty().MaximumLength(256);
    }

    private static async Task<IResult> ListUsers(
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = userManager.Users.AsNoTracking().OrderBy(u => u.Email);
        var total = await query.LongCountAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var users = await query
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(u => new UserSummary(
                u.Id, u.Email, u.UserName, u.EmailConfirmed, u.TwoFactorEnabled,
                u.LockoutEnd != null && u.LockoutEnd > now))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedList<UserSummary>(users, pagination.Page, pagination.PageSize, total));
    }

    private static async Task<IResult> ListRoles(
        RoleManager<ApplicationRole> roleManager, CancellationToken cancellationToken)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync(cancellationToken);

        return Results.Ok(roles);
    }

    private static IResult ListPermissions(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        var permissions = providers
            .SelectMany(p => p.GetPermissions())
            .DistinctBy(p => p.Name)
            .GroupBy(p => p.Group)
            .Select(g => new { group = g.Key, permissions = g.OrderBy(p => p.Name) });

        return Results.Ok(permissions);
    }

    private static async Task<IResult> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ICurrentUser currentUser,
        ISecurityAuditLogger audit)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound(new { message = "User not found." });

        if (!await roleManager.RoleExistsAsync(request.Role))
            return Results.NotFound(new { message = "Role not found." });

        if (!await userManager.IsInRoleAsync(user, request.Role))
        {
            var result = await userManager.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = result.Errors.Select(e => e.Description).ToArray()
                });

            audit.RoleAssigned(user.Id, request.Role, currentUser.UserId);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveRole(
        Guid userId,
        string role,
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser,
        ISecurityAuditLogger audit)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.NotFound(new { message = "User not found." });

        if (await userManager.IsInRoleAsync(user, role))
        {
            await userManager.RemoveFromRoleAsync(user, role);
            audit.RoleRemoved(user.Id, role, currentUser.UserId);
        }

        return Results.NoContent();
    }
}
