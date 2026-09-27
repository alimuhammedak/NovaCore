using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;
using NovaCore.AspNetCore.Authorization;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Identity.OpenIddict;

/// <summary>Service registration for the OpenID Connect provider.</summary>
public static class IdentityServerHostingExtensions
{
    /// <summary>
    /// Adds an OpenID Connect provider to the host: ASP.NET Core Identity stores the accounts and OpenIddict serves the
    /// protocol (Authorization Code with PKCE, refresh tokens, client credentials) from <c>/connect/*</c>. Its issuer is
    /// <c>Jwt:Authority</c>, so the core validates its access tokens like any other provider's: through discovery and
    /// the published keys. The access tokens are signed JWTs for the <c>Jwt:Audience</c> API. The permission resolver
    /// reads the store, so users get the permissions of their roles.
    /// </summary>
    /// <remarks>
    /// The host registers <see cref="IdentityServerDbContext"/> with its database provider (<c>AddModuleDbContext</c>) and
    /// keeps its migrations. In Development the signing and encryption keys are ephemeral; anywhere else the host adds
    /// its certificates with OpenIddict's own API (<c>services.AddOpenIddict().AddServer(...)</c>), and the host does not
    /// start without them.
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCoreIdentityServer(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityServerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<IdentityServerDbContext>())
            .AddServer(server =>
            {
                server
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetRevocationEndpointUris("connect/revoke")
                    .SetEndSessionEndpointUris("connect/endsession");

                server
                    .AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                // S256 only: a "plain" challenge is the verifier itself, so it protects nothing once it leaks (RFC 9700 §2.1.1).
                server.Configure(options => options.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain));

                server.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.Roles, Scopes.OfflineAccess);

                // The core validates access tokens like a third-party resource server: a signed JWT (RFC 9068), which it
                // checks against the published keys (docs/plan/RESEARCH.md R-059).
                server.DisableAccessTokenEncryption();

                // Short-lived access tokens (OWASP); rolling refresh tokens keep the session going.
                server
                    .SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

                if (builder.Environment.IsDevelopment())
                    server.AddEphemeralEncryptionKey().AddEphemeralSigningKey();

                server.UseAspNetCore();
            });

        // The issuer is the authority the core validates against, so the "iss" of every token and the discovery
        // document match it, also behind a proxy. ValidateOnStart builds the options when the host starts, so missing
        // keys (OpenIddict's own check) or a missing authority stop the host instead of the first sign-in.
        services.AddOptions<OpenIddictServerOptions>()
            .Configure<IOptions<JwtOptions>>((server, jwt) =>
            {
                if (Uri.TryCreate(jwt.Value.Authority, UriKind.Absolute, out var issuer))
                    server.Issuer = issuer;
            })
            .Validate(
                server => server.Issuer is not null,
                "NovaCore.Identity.OpenIddict issues the tokens that Jwt:Authority points at, so Jwt:Authority must be "
                + "the absolute public URL of this host, e.g. https://localhost:5443.")
            .ValidateOnStart();

        services.AddScoped<IPermissionResolver, IdentityPermissionResolver>();

        return builder;
    }
}
