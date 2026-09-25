using System.Buffers.Text;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NovaCore.FluentValidation;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity.Features;

/// <summary>Self-service registration and email confirmation. Anti-enumeration by design.</summary>
public static class RegistrationEndpoints
{
    private static readonly object GenericAccepted =
        new { message = "If the email address is new, a confirmation link has been sent." };

    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new account (sends an email confirmation link)")
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AllowAnonymous();

        group.MapGet("/confirm-email", ConfirmEmail)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm an email address using the token from the confirmation link")
            .AllowAnonymous();
    }

    public sealed record RegisterRequest(string Email, string Password, string? DisplayName);

    public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            // Length-first policy (OWASP): require length, not character composition.
            RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
            RuleFor(x => x.DisplayName).MaximumLength(128);
        }
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ISecurityAuditLogger audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // If the only problem is that the user already exists, don't reveal it — return the same
            // generic response a brand-new registration gets.
            var onlyDuplicate = result.Errors.All(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
            if (onlyDuplicate)
                return Results.Ok(GenericAccepted);

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        await userManager.AddToRoleAsync(user, IdentityDataSeeder.UserRole);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));
        var link = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}" +
                   $"/api/v1/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        await emailSender.SendAsync(request.Email, "Confirm your NovaCore account",
            $"Welcome! Confirm your email: <a href=\"{link}\">Confirm</a>", cancellationToken);

        audit.UserRegistered(user.Id, request.Email);
        return Results.Ok(GenericAccepted);
    }

    private static async Task<IResult> ConfirmEmail(
        Guid userId,
        string token,
        UserManager<ApplicationUser> userManager,
        ISecurityAuditLogger audit)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Results.BadRequest(new { message = "Invalid or expired confirmation token." });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        }
        catch (FormatException)
        {
            return Results.BadRequest(new { message = "Invalid or expired confirmation token." });
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return Results.BadRequest(new { message = "Invalid or expired confirmation token." });

        audit.EmailConfirmed(user.Id);
        return Results.Ok(new { message = "Email confirmed. You can now sign in." });
    }
}
