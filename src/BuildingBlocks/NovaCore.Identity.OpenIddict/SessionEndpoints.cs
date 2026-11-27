using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The sign-in a project's own screen uses instead of the HTML pages (docs/plan/DECISIONS.md D-090, D-091 (1), (2)):
/// the same rules (<see cref="SignInFlow"/>), JSON in and out. A success opens the provider's session cookie and names
/// where to go next, the authorization request; the tokens still come only from <c>/connect/token</c> with PKCE.
/// The screen is served from this origin: the group answers no cross-origin request (RFC 9700 §2.6) and every POST
/// needs the antiforgery token in a header.
/// </summary>
internal static class SessionEndpoints
{
    public const string AntiforgeryHeader = "X-XSRF-TOKEN";

    private const string SignedIn = "signed_in";
    private const string TwoFactorRequired = "two_factor_required";

    // One answer for an unknown account, a wrong password and a locked account (D-030 (1)).
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("account.invalid_credentials", "Invalid email or password.");

    private static readonly Error EmailNotConfirmed =
        Error.Forbidden("account.email_not_confirmed", "Confirm your email address before signing in.");

    private static readonly Error InvalidCode =
        Error.Unauthorized("account.invalid_code", "Invalid authenticator or recovery code.");

    private static readonly Error NoTwoFactorSession =
        Error.Unauthorized("account.two_factor_session_missing", "Sign in with the password first.");

    private static readonly Error AntiforgeryInvalid =
        Error.Validation("account.antiforgery_invalid", $"Send the token from GET /api/v1/auth/session/antiforgery in the {AntiforgeryHeader} header.");

    /// <summary>The antiforgery token to send back in <see cref="HeaderName"/>.</summary>
    internal sealed record AntiforgeryTokenResponse(string Token, string HeaderName);

    /// <summary><c>signed_in</c> or <c>two_factor_required</c>, and the local URL to go to next, if the request named one.</summary>
    internal sealed record SessionSignInResponse(string Status, string? ReturnUrl);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var session = endpoints.MapGroup("/api/v1/auth/session").WithTags("Auth").RequireRateLimiting("strict")
            .AllowAnonymous().WithMetadata(new DisableCorsAttribute());

        session.MapGet("/antiforgery", Antiforgery)
            .WithName("SessionAntiforgery")
            .WithSummary("Get the antiforgery token the sign-in requests send back in a header");

        session.MapPost("/login", SignIn)
            .WithName("SessionSignIn")
            .WithSummary("Sign in with email and password; opens the provider's session, returns no token")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        session.MapPost("/login/2fa", SignInWithCode)
            .WithName("SessionSignInTwoFactor")
            .WithSummary("Finish signing in with an authenticator or recovery code")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static Ok<AntiforgeryTokenResponse> Antiforgery(HttpContext context, IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, options.Value.HeaderName!));
    }

    private static async Task<Results<Ok<SessionSignInResponse>, ProblemHttpResult>> SignIn(
        SessionSignInRequest request, HttpContext context, IAntiforgery antiforgery, SignInFlow flow)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return ResultHttpExtensions.Problem(AntiforgeryInvalid);

        return await flow.PasswordAsync(request.Email, request.Password, context.Connection.RemoteIpAddress?.ToString()) switch
        {
            SignInOutcome.Succeeded => TypedResults.Ok(new SessionSignInResponse(SignedIn, request.ReturnUrl)),
            SignInOutcome.TwoFactorRequired => TypedResults.Ok(new SessionSignInResponse(TwoFactorRequired, request.ReturnUrl)),
            SignInOutcome.EmailNotConfirmed => ResultHttpExtensions.Problem(EmailNotConfirmed),
            _ => ResultHttpExtensions.Problem(InvalidCredentials),
        };
    }

    private static async Task<Results<Ok<SessionSignInResponse>, ProblemHttpResult>> SignInWithCode(
        SessionCodeRequest request, HttpContext context, IAntiforgery antiforgery, SignInFlow flow)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return ResultHttpExtensions.Problem(AntiforgeryInvalid);

        return await flow.CodeAsync(request.Code, context.Connection.RemoteIpAddress?.ToString()) switch
        {
            SignInOutcome.Succeeded => TypedResults.Ok(new SessionSignInResponse(SignedIn, request.ReturnUrl)),
            SignInOutcome.NoTwoFactorSession => ResultHttpExtensions.Problem(NoTwoFactorSession),
            _ => ResultHttpExtensions.Problem(InvalidCode),
        };
    }
}
