using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Identity;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity.Features;

/// <summary>TOTP (authenticator-app) two-factor: set up the shared key, enable with a code, disable.</summary>
public static class TwoFactorEndpoints
{
    private const string Issuer = "NovaCore";

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/2fa/setup", Setup)
            .WithName("TwoFactorSetup").WithSummary("Get the authenticator shared key + otpauth URI")
            .RequireAuthorization();

        group.MapPost("/2fa/enable", Enable)
            .WithName("TwoFactorEnable").WithSummary("Enable 2FA by verifying a TOTP code (returns recovery codes)")
            .RequireAuthorization();

        group.MapPost("/2fa/disable", Disable)
            .WithName("TwoFactorDisable").WithSummary("Disable 2FA after verifying a TOTP code")
            .RequireAuthorization();
    }

    public sealed record CodeRequest([Required, Length(6, 8)] string Code);
    public sealed record SetupResponse(string SharedKey, string AuthenticatorUri);

    private static async Task<IResult> Setup(
        UserManager<ApplicationUser> userManager, ICurrentUser currentUser)
    {
        var user = await FindAsync(userManager, currentUser);
        if (user is null)
            return Results.Unauthorized();

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? user.Id.ToString();
        var uri = $"otpauth://totp/{Uri.EscapeDataString($"{Issuer}:{email}")}" +
                  $"?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";

        return Results.Ok(new SetupResponse(key!, uri));
    }

    private static async Task<IResult> Enable(
        CodeRequest request, UserManager<ApplicationUser> userManager, ICurrentUser currentUser, ISecurityAuditLogger audit)
    {
        var user = await FindAsync(userManager, currentUser);
        if (user is null)
            return Results.Unauthorized();

        var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!valid)
            return InvalidCode();

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        audit.TwoFactorEnabled(user.Id);

        return Results.Ok(new { recoveryCodes = recoveryCodes?.ToArray() ?? [] });
    }

    private static async Task<IResult> Disable(
        CodeRequest request, UserManager<ApplicationUser> userManager, ICurrentUser currentUser, ISecurityAuditLogger audit)
    {
        var user = await FindAsync(userManager, currentUser);
        if (user is null)
            return Results.Unauthorized();

        var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!valid)
            return InvalidCode();

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        audit.TwoFactorDisabled(user.Id);

        return Results.NoContent();
    }

    private static Task<ApplicationUser?> FindAsync(UserManager<ApplicationUser> userManager, ICurrentUser currentUser) =>
        Guid.TryParse(currentUser.UserId, out var id)
            ? userManager.FindByIdAsync(id.ToString())
            : Task.FromResult<ApplicationUser?>(null);

    private static IResult InvalidCode() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["code"] = ["The verification code is invalid."]
    });
}
