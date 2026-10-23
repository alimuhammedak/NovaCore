using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Auth;
using NovaCore.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The protocol endpoints OpenIddict hands over after validating the request (passthrough): authorization, token and
/// end session, which asks a signed-in browser before it signs it out. The revocation endpoint is OpenIddict's own.
/// </summary>
internal static class ConnectEndpoints
{
    private const string AuthenticationTimeItem = ".auth_time";

    private const string EndSessionPath = "/connect/endsession";

    // Anonymous to the core's authorization: OpenIddict has validated the request and the client, and each handler
    // reads the sign-in session itself.
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        // A browser navigates here; no script on another origin calls it (RFC 9700 §2.6, docs/plan/RESEARCH.md R-102).
        endpoints.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], Authorize)
            .ExcludeFromDescription().AllowAnonymous().WithMetadata(new DisableCorsAttribute());
        endpoints.MapPost("/connect/token", Exchange)
            .ExcludeFromDescription().AllowAnonymous().RequireRateLimiting("strict");
        endpoints.MapMethods(EndSessionPath, [HttpMethods.Get, HttpMethods.Post], EndSession)
            .ExcludeFromDescription().AllowAnonymous();
    }

    [SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling",
        Justification = "The authorization endpoint is one linear OpenID Connect flow over the request, the session, the client, the user and the authorization; splitting it would scatter the protocol's steps (docs/plan/RESEARCH.md R-117).")]
    private static async Task<IResult> Authorize(
        HttpContext context,
        UserManager<ApplicationUser> users,
        IOpenIddictApplicationManager applications,
        IOpenIddictAuthorizationManager authorizations,
        IOptions<JwtOptions> jwt,
        TimeProvider timeProvider)
    {
        var request = context.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var session = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var user = session.Succeeded ? await users.GetUserAsync(session.Principal) : null;

        // OpenID Connect Core §3.1.2.1: prompt=login and max_age ask for a fresh sign-in, prompt=none for none at all.
        if (user is null || request.HasPromptValue(PromptValues.Login) || IsTooOld(session, request, timeProvider))
        {
            if (request.HasPromptValue(PromptValues.None))
                return Forbid(Errors.LoginRequired, "The user is not signed in.");

            // Back here after signing in, without the parameters that asked for it, so the fresh session is accepted.
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = RequestUriWithout(context.Request, Parameters.Prompt, Parameters.MaxAge) },
                [IdentityConstants.ApplicationScheme]);
        }

        var application = await applications.FindByClientIdAsync(request.ClientId!)
                          ?? throw new InvalidOperationException("The client application cannot be found.");
        var applicationId = await applications.GetIdAsync(application);
        var subject = user.Id.ToString();

        var identity = await CreateIdentityAsync(users, user, request.GetScopes(), jwt.Value.Audience);
        if (AuthenticationTime(session) is { } signedInAt)
            identity.SetClaim(Claims.AuthenticationTime, signedInAt.ToUnixTimeSeconds());

        // Implicit consent (first-party clients): reuse the user's permanent authorization for this client, or create
        // one. Its id ties every token of the session together, for chain revocation on refresh token reuse.
        var authorization = await authorizations
                                .FindAsync(subject, applicationId, Statuses.Valid, AuthorizationTypes.Permanent, identity.GetScopes())
                                .LastOrDefaultAsync()
                            ?? await authorizations.CreateAsync(
                                identity, subject, applicationId!, AuthorizationTypes.Permanent, identity.GetScopes());

        identity.SetAuthorizationId(await authorizations.GetIdAsync(authorization));
        identity.SetDestinations(GetDestinations);

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> Exchange(
        HttpContext context,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IOpenIddictApplicationManager applications,
        IOptions<JwtOptions> jwt,
        SecurityAuditLogger audit)
    {
        var request = context.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
            return await ServiceTokenAsync(request, applications, jwt.Value.Audience);

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            throw new InvalidOperationException("The grant type is not supported.");

        // OpenIddict has validated the code or refresh token; the principal is the one stored in it.
        var principal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

        // The account may have changed since: it must still exist and be allowed to sign in.
        var user = await users.FindByIdAsync(principal.GetClaim(Claims.Subject)!);
        if (user is null)
            return Forbid(Errors.InvalidGrant, "The token is no longer valid.");

        if (!await signIn.CanSignInAsync(user) || await users.IsLockedOutAsync(user))
            return Forbid(Errors.InvalidGrant, "The user is no longer allowed to sign in.");

        // Fresh claims (a role change reaches the next token), keeping the scopes, resources and authorization. The
        // principal went through the core's claims transformation like every authenticated one, so it carries resolved
        // permissions; they stay out of the token (they are resolved per request).
        var identity = new ClaimsIdentity(
            principal.Claims.Where(claim => claim.Type != NovaClaims.Permission),
            TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        SetUserClaims(identity, user, await users.GetRolesAsync(user));
        identity.SetDestinations(GetDestinations);

        if (request.IsRefreshTokenGrantType())
            audit.TokenRefreshed(user.Id, context.Connection.RemoteIpAddress?.ToString());

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // OpenIddict has authenticated the client with its secret. The token speaks for the client itself: its "sub" is the
    // client id, which the permission resolver maps to the client's configured permissions.
    private static async Task<IResult> ServiceTokenAsync(
        OpenIddictRequest request, IOpenIddictApplicationManager applications, string audience)
    {
        var application = await applications.FindByClientIdAsync(request.ClientId!)
                          ?? throw new InvalidOperationException("The client application cannot be found.");

        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity
            .SetClaim(Claims.Subject, request.ClientId)
            .SetClaim(Claims.Name, await applications.GetDisplayNameAsync(application) ?? request.ClientId);
        identity.SetScopes(request.GetScopes());
        identity.SetResources(audience);
        identity.SetDestinations(GetDestinations);

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> EndSession(
        HttpContext context,
        IAntiforgery antiforgery,
        IOptions<AntiforgeryOptions> antiforgeryOptions,
        UserManager<ApplicationUser> users,
        SecurityAuditLogger audit)
    {
        var session = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var user = session.Succeeded ? await users.GetUserAsync(session.Principal) : null;

        // A signed-in browser is asked first, so a link or a form on another site can't sign it out (logout CSRF;
        // RP-Initiated Logout 1.0 §2 asks for the question when there's no id_token_hint, docs/plan/RESEARCH.md R-086).
        // Only the question's own form carries the antiforgery token. IsRequestValidAsync answers true for a GET, hence
        // the method check. A signed-out browser has nothing to lose and goes straight on.
        if (user is not null && !(HttpMethods.IsPost(context.Request.Method) && await antiforgery.IsRequestValidAsync(context)))
            return await SignOutQuestionAsync(context, antiforgery, antiforgeryOptions.Value.FormFieldName);

        if (user is not null)
            audit.SignedOut(user.Id);

        await context.SignOutAsync(IdentityConstants.ApplicationScheme);

        // OpenIddict redirects to the post_logout_redirect_uri when the client registered it.
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    // The question posts the end-session request's own parameters back (client_id, post_logout_redirect_uri, state, ...),
    // so OpenIddict validates the confirmed request like the first one (OpenIddict's samples do the same).
    private static async Task<IResult> SignOutQuestionAsync(HttpContext context, IAntiforgery antiforgery, string tokenField)
    {
        var parameters = context.Request.HasFormContentType
            ? (IEnumerable<KeyValuePair<string, StringValues>>)await context.Request.ReadFormAsync(context.RequestAborted)
            : context.Request.Query;

        var hidden = parameters
            .Where(parameter => !string.Equals(parameter.Key, tokenField, StringComparison.Ordinal))
            .SelectMany(parameter => parameter.Value.Select(value => new KeyValuePair<string, string?>(parameter.Key, value)));

        return HtmlPage.Form(context, antiforgery, "Sign out", EndSessionPath, hidden, error: null,
            "  <p>Do you want to sign out?</p>\n", submit: "Sign out");
    }

    private static async Task<ClaimsIdentity> CreateIdentityAsync(
        UserManager<ApplicationUser> users, ApplicationUser user, ImmutableArray<string> scopes, string audience)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        SetUserClaims(identity, user, await users.GetRolesAsync(user));
        identity.SetScopes(scopes);

        // One API: every token is for the audience the core validates (Jwt:Audience).
        identity.SetResources(audience);
        return identity;
    }

    // The claim names the core reads (NovaClaims). Permissions are not claims: the core resolves them per request.
    private static void SetUserClaims(ClaimsIdentity identity, ApplicationUser user, IList<string> roles)
    {
        identity
            .SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Name, user.UserName ?? user.Email ?? user.Id.ToString())
            .SetClaim(Claims.Email, user.Email)
            .SetClaims(Claims.Role, [.. roles]);
    }

    // The access token carries what the core reads; the identity token what the granted scopes ask for.
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        yield return claim.Type switch
        {
            Claims.AuthenticationTime => Destinations.IdentityToken,
            _ => Destinations.AccessToken
        };

        var scope = claim.Type switch
        {
            Claims.Subject => Scopes.OpenId,
            Claims.Name => Scopes.Profile,
            Claims.Email => Scopes.Email,
            Claims.Role => Scopes.Roles,
            _ => null
        };

        if (scope is not null && claim.Subject!.HasScope(scope))
            yield return Destinations.IdentityToken;
    }

    // The cookie's IssuedUtc is no sign-in time: sliding expiration and each security stamp check renew the cookie and
    // move it (docs/plan/RESEARCH.md R-067). The sign-in time is recorded in the cookie's properties instead, which a
    // renewal keeps.
    internal static void RecordAuthenticationTime(AuthenticationProperties properties) =>
        properties.Items[AuthenticationTimeItem] =
            properties.IssuedUtc!.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    private static DateTimeOffset? AuthenticationTime(AuthenticateResult session) =>
        session.Properties?.Items.TryGetValue(AuthenticationTimeItem, out var value) == true
        && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    // A session without a recorded sign-in time can't prove it is recent enough.
    private static bool IsTooOld(AuthenticateResult session, OpenIddictRequest request, TimeProvider timeProvider) =>
        request.MaxAge is { } maxAge
        && (AuthenticationTime(session) is not { } signedInAt || timeProvider.GetUtcNow() - signedInAt > TimeSpan.FromSeconds(maxAge));

    private static string RequestUriWithout(HttpRequest request, params string[] parameters)
    {
        var values = request.HasFormContentType ? (IEnumerable<KeyValuePair<string, StringValues>>)request.Form : request.Query;
        var kept = values.Where(pair => !parameters.Contains(pair.Key, StringComparer.Ordinal));
        return request.PathBase + request.Path + QueryString.Create(kept);
    }

    private static IResult Forbid(string error, string description) => Results.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
