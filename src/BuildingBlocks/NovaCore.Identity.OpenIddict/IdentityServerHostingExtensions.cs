using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// gives users the permissions of their roles (from the store) and service clients the permissions configured for
    /// them (<c>Identity:Clients</c>).
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
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                // OWASP Authentication Cheat Sheet: length first, no character-composition rules.
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Brute-force throttling per account, new accounts included.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityServerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // OWASP Password Storage Cheat Sheet: at least 600,000 iterations for PBKDF2-HMAC-SHA256.
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);

        // Email confirmation tokens expire after six hours.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(6));

        services.AddSingleton<SecurityAuditLogger>();

        // LoggingEmailSender writes confirmation links to the log: Development only, and only when the host brings none.
        // Anywhere else the host registers a real sender, or EmailSenderStartupCheck stops the host.
        if (builder.Environment.IsDevelopment())
            services.TryAddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<EmailSenderStartupCheck>();

        // Built-in validation is generated per assembly, so the add-on registers it for its own requests (R-052).
        services.AddValidation();

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
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

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

                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });

        // The issuer is the authority the core validates against (validated there: absolute, https outside Development),
        // so the "iss" of every token and the discovery document match it, also behind a proxy. ValidateOnStart builds the
        // options when the host starts, so missing keys (OpenIddict's own check) stop the host instead of the first sign-in.
        services.AddOptions<OpenIddictServerOptions>()
            .Configure<IOptions<JwtOptions>>((server, jwt) => server.Issuer = new Uri(jwt.Value.Authority, UriKind.Absolute))
            .ValidateOnStart();

        // The sign-in session at the provider: Identity's cookies, alongside the API's bearer default (the parameterless
        // AddAuthentication doesn't change the default scheme, docs/plan/RESEARCH.md R-061). Only /connect/* and
        // /account/* read it.
        services.AddAuthentication().AddIdentityCookies(cookies => cookies.ApplicationCookie!.Configure(cookie =>
        {
            cookie.LoginPath = LoginPage.Path;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            cookie.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            cookie.SlidingExpiration = true;
        }));
        services.AddAntiforgery();

        // The cookie is read only at /connect/* and /account/*, so checking its security stamp on every read is cheap, and
        // a password change or "sign out everywhere" ends the provider session at once instead of within 30 minutes.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

        services.AddOptions<IdentityServerOptions>()
            .BindConfiguration(IdentityServerOptions.SectionName)
            .Validate(
                identity => identity.Clients.TrueForAll(client => !Guid.TryParse(client.ClientId, out _)),
                "Identity:Clients:n:ClientId must not be a Guid: a Guid subject is a user.")
            .Validate(
                identity => identity.Clients.DistinctBy(client => client.ClientId, StringComparer.Ordinal).Count() == identity.Clients.Count,
                "Identity:Clients:n:ClientId must be unique.")
            .Validate(
                identity => identity.Clients.TrueForAll(client =>
                    client.RedirectUris.Concat(client.PostLogoutRedirectUris).All(uri => uri.IsAbsoluteUri)),
                "Identity:Clients:n:RedirectUris and PostLogoutRedirectUris must be absolute URIs.")
            .Validate(
                identity => identity.Clients.TrueForAll(client => client.SignsInUsers || !string.IsNullOrEmpty(client.ClientSecret)),
                "Identity:Clients:n without RedirectUris is a service client (client credentials) and needs a ClientSecret.")
            .Validate(
                identity => identity.Clients.TrueForAll(client => !client.SignsInUsers || client.Permissions.Count == 0),
                "Identity:Clients:n:Permissions apply to service clients only: a client with RedirectUris acts for its users, "
                + "who get their own permissions.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<IdentityServerOptions>, IdentityServerOptionsValidator>());
        services.AddHostedService<IdentityServerSeeder>();

        services.AddScoped<IPermissionResolver, IdentityPermissionResolver>();
        services.AddPermissionDefinitions<IdentityPermissionProvider>();

        return builder;
    }

    /// <summary>
    /// Maps the provider's endpoints: the authorization, token and end-session endpoints under <c>/connect</c> (OpenIddict
    /// validates the requests first) and the sign-in page at <c>/account/login</c>, which stay out of the OpenAPI
    /// document; and the account API under <c>/api/v1/auth</c> (registration, email confirmation, the current user,
    /// password change, sign-out everywhere, two-factor authentication) and the user and role administration under
    /// <c>/api/v1/admin</c>, guarded by the <c>identity:*</c> permissions. The token endpoint, the sign-in page and the
    /// account API use the <c>strict</c> rate limit.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapCoreIdentityServer(this IEndpointRouteBuilder endpoints)
    {
        ConnectEndpoints.Map(endpoints);
        LoginPage.Map(endpoints);
        AccountEndpoints.Map(endpoints);
        AdminEndpoints.Map(endpoints);
        return endpoints;
    }
}
