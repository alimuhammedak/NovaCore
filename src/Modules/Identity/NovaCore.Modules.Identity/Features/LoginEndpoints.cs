using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NovaCore.AspNetCore.Http;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity.Features;

/// <summary>
/// Login, two-factor login, and token refresh. Failures return a uniform 401 to prevent user
/// enumeration; lockout and reuse are handled by Identity / the token service and audited.
/// </summary>
public static class LoginEndpoints
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/login", Login)
            .WithName("Login").WithSummary("Sign in with email + password")
            .AllowAnonymous();

        group.MapPost("/login/2fa", TwoFactorLogin)
            .WithName("TwoFactorLogin").WithSummary("Complete sign-in with a TOTP code")
            .AllowAnonymous();

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken").WithSummary("Exchange a refresh token for a new token pair (rotating)")
            .AllowAnonymous();
    }

    public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
    public sealed record TwoFactorLoginRequest(
        [Required, EmailAddress] string Email, [Required] string Password, [Required, Length(6, 8)] string Code);
    public sealed record RefreshRequest([Required] string RefreshToken);

    private static async Task<IResult> Login(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService tokenService,
        ISecurityAuditLogger audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = ClientIp(httpContext);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            audit.LoginFailed(request.Email, ip, "unknown-user");
            return InvalidCredentials();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            audit.AccountLockedOut(user.Id, ip);
            return InvalidCredentials();
        }

        if (result.IsNotAllowed)
        {
            // Password was correct but the account can't sign in yet (email not confirmed).
            audit.LoginFailed(request.Email, ip, "email-not-confirmed");
            return Results.Problem(
                title: "Email not confirmed",
                detail: "Please confirm your email address before signing in.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = "auth.email_not_confirmed" });
        }

        if (result.RequiresTwoFactor)
        {
            audit.TwoFactorChallenged(user.Id);
            return Results.Ok(new { requiresTwoFactor = true });
        }

        if (!result.Succeeded)
        {
            audit.LoginFailed(request.Email, ip, "bad-password");
            return InvalidCredentials();
        }

        var tokens = await tokenService.IssueAsync(user, ip, cancellationToken);
        audit.LoginSucceeded(user.Id, ip);
        return Results.Ok(tokens);
    }

    private static async Task<IResult> TwoFactorLogin(
        TwoFactorLoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuthTokenService tokenService,
        ISecurityAuditLogger audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ip = ClientIp(httpContext);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return InvalidCredentials();

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (passwordResult.IsLockedOut)
        {
            audit.AccountLockedOut(user.Id, ip);
            return InvalidCredentials();
        }

        if (!passwordResult.Succeeded && !passwordResult.RequiresTwoFactor)
            return InvalidCredentials();

        var codeValid = await userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, request.Code);

        if (!codeValid)
        {
            audit.LoginFailed(request.Email, ip, "bad-2fa-code");
            return InvalidCredentials();
        }

        var tokens = await tokenService.IssueAsync(user, ip, cancellationToken);
        audit.LoginSucceeded(user.Id, ip);
        return Results.Ok(tokens);
    }

    private static async Task<IResult> Refresh(
        RefreshRequest request,
        IAuthTokenService tokenService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await tokenService.RefreshAsync(request.RefreshToken, ClientIp(httpContext), cancellationToken);
        return result.ToHttpResult();
    }

    private static string? ClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString();

    private static IResult InvalidCredentials() => Results.Problem(
        title: "Authentication failed",
        detail: "Invalid credentials.",
        statusCode: StatusCodes.Status401Unauthorized,
        extensions: new Dictionary<string, object?> { ["code"] = "auth.invalid_credentials" });
}
