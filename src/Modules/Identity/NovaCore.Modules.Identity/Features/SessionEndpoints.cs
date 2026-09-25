using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Identity;
using NovaCore.AspNetCore.Validation;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity.Features;

/// <summary>Authenticated session operations: who-am-I, logout, logout-everywhere, change password.</summary>
public static class SessionEndpoints
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/me", Me)
            .WithName("Me").WithSummary("Current principal: id, email, roles, permissions")
            .RequireAuthorization();

        group.MapPost("/logout", Logout)
            .WithName("Logout").WithSummary("Revoke the presented refresh token's session")
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>()
            .RequireAuthorization();

        group.MapPost("/logout-all", LogoutAll)
            .WithName("LogoutAll").WithSummary("Revoke every refresh token for the current user")
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword").WithSummary("Change password (revokes all sessions)")
            .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>()
            .RequireAuthorization();
    }

    public sealed record LogoutRequest(string RefreshToken);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public sealed record MeResponse(string UserId, string? UserName, string? Email, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);

    public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128);
        }
    }

    public sealed class LogoutValidator : AbstractValidator<LogoutRequest>
    {
        public LogoutValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
    }

    private static IResult Me(ICurrentUser currentUser) =>
        currentUser is { IsAuthenticated: true, UserId: { } userId }
            ? Results.Ok(new MeResponse(userId, currentUser.UserName, currentUser.Email, currentUser.Roles, currentUser.Permissions))
            : Results.Unauthorized();

    private static async Task<IResult> Logout(
        LogoutRequest request,
        IAuthTokenService tokenService,
        ICurrentUser currentUser,
        ISecurityAuditLogger audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await tokenService.RevokeAsync(request.RefreshToken, httpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        if (Guid.TryParse(currentUser.UserId, out var userId))
            audit.LoggedOut(userId, allSessions: false);

        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAll(
        IAuthTokenService tokenService,
        ICurrentUser currentUser,
        ISecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Results.Unauthorized();

        await tokenService.RevokeAllAsync(userId, cancellationToken);
        audit.LoggedOut(userId, allSessions: true);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService tokenService,
        ICurrentUser currentUser,
        ISecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        // Defence in depth: a password change invalidates every existing session.
        await tokenService.RevokeAllAsync(userId, cancellationToken);
        audit.PasswordChanged(userId);
        return Results.NoContent();
    }
}
