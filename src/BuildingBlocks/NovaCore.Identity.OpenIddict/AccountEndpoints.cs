using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The account API under <c>/api/v1/auth</c> (the Identity module's paths): self-service registration and email
/// confirmation. Anti-enumeration by design: registering an existing address looks like registering a new one.
/// </summary>
internal static class AccountEndpoints
{
    public const string ConfirmEmailPath = "api/v1/auth/confirm-email";

    private static readonly object GenericAccepted =
        new { message = "If the email address is new, a confirmation link has been sent." };

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Auth").RequireRateLimiting("strict");

        auth.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new account (sends an email confirmation link)")
            .AllowAnonymous();

        auth.MapGet("/confirm-email", ConfirmEmail)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm an email address using the token from the confirmation link")
            .AllowAnonymous();

        return auth;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> users,
        IEmailSender emailSender,
        SecurityAuditLogger audit,
        IOptions<JwtOptions> jwt,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // An existing account gets the answer a new one gets: the response doesn't reveal it.
            if (result.Errors.All(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                return Results.Ok(GenericAccepted);

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(error => error.Description).ToArray()
            });
        }

        var token = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));

        // The link starts from the configured public URL, never from the request's Host header (host header poisoning).
        var link = QueryHelpers.AddQueryString(
            new Uri(new Uri(jwt.Value.Authority.TrimEnd('/') + "/"), ConfirmEmailPath).AbsoluteUri,
            new Dictionary<string, string?> { ["userId"] = user.Id.ToString(), ["token"] = token });

        await emailSender.SendAsync(request.Email, "Confirm your account",
            $"Welcome! Confirm your email address: <a href=\"{link}\">Confirm</a>", cancellationToken);

        audit.UserRegistered(user.Id, request.Email);
        return Results.Ok(GenericAccepted);
    }

    private static async Task<IResult> ConfirmEmail(
        Guid userId,
        string token,
        UserManager<ApplicationUser> users,
        SecurityAuditLogger audit)
    {
        var invalid = Results.BadRequest(new { message = "Invalid or expired confirmation token." });

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return invalid;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        }
        catch (FormatException)
        {
            return invalid;
        }

        if (!(await users.ConfirmEmailAsync(user, decoded)).Succeeded)
            return invalid;

        audit.EmailConfirmed(user.Id);
        return Results.Ok(new { message = "Email confirmed. You can now sign in." });
    }
}
