using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
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
/// end session. The revocation endpoint is OpenIddict's own.
/// </summary>
internal static class ConnectEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], Authorize).ExcludeFromDescription();
        endpoints.MapPost("/connect/token", Exchange).ExcludeFromDescription().RequireRateLimiting("strict");
        endpoints.MapMethods("/connect/endsession", [HttpMethods.Get, HttpMethods.Post], (Delegate)EndSession).ExcludeFromDescription();
    }

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
        if (session.Properties?.IssuedUtc is { } signedInAt)
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
        IOptions<JwtOptions> jwt)
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

    private static async Task<IResult> EndSession(HttpContext context)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);

        // OpenIddict redirects to the post_logout_redirect_uri when the client registered it.
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
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

    private static bool IsTooOld(AuthenticateResult session, OpenIddictRequest request, TimeProvider timeProvider) =>
        request.MaxAge is { } maxAge
        && session.Properties?.IssuedUtc is { } signedInAt
        && timeProvider.GetUtcNow() - signedInAt > TimeSpan.FromSeconds(maxAge);

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
