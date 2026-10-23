using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
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
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling",
    Justification = "The provider's composition root registers ASP.NET Core Identity, OpenIddict, the cookies, CORS, options and the add-on's services, so it names their types; each registration stays a few lines (docs/plan/RESEARCH.md R-117).")]
public static class IdentityServerHostingExtensions
{
    /// <summary>
    /// The key of the provider's OAuth 2.0 (Authorization Code with PKCE) security scheme in the OpenAPI document, for an
    /// API explorer to preselect, e.g. Scalar's <c>AddPreferredSecuritySchemes</c> and <c>AddAuthorizationCodeFlow</c>.
    /// </summary>
    public const string OpenApiSecuritySchemeId = "OAuth2";

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
    /// start without them. Outside Development the host also chooses where the Data Protection keys behind the sign-in
    /// cookies, antiforgery tokens and confirmation links live (e.g. <c>services.AddDataProtection().PersistKeysToFileSystem(...)</c>);
    /// the host does not start without a key store either.
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCoreIdentityServer(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        AddAccountStore(services);
        services.AddSingleton<SecurityAuditLogger>();
        services.AddSingleton<DummyPasswordHash>();

        AddEmail(services, builder.Environment);

        // Built-in validation is generated per assembly, so the add-on registers it for its own requests (R-052).
        services.AddValidation();

        AddOpenIddictServer(services, builder.Environment);

        // The issuer is the authority the core validates against (validated there: absolute, https outside Development),
        // so the "iss" of every token and the discovery document match it, also behind a proxy. ValidateOnStart builds the
        // options when the host starts, so missing keys (OpenIddict's own check) stop the host instead of the first sign-in.
        services.AddOptions<OpenIddictServerOptions>()
            .Configure<IOptions<JwtOptions>>((server, jwt) => server.Issuer = new Uri(jwt.Value.Authority, UriKind.Absolute))
            .ValidateOnStart();

        // This provider's access tokens are RFC 9068 JWTs ("typ": "at+jwt"), and §4 has the resource server check the
        // type, so no other JWT signed with the same key (an identity token) passes for one. Here, not in the core: the
        // core validates other providers too, and Microsoft Entra's access tokens say "JWT" (docs/plan/RESEARCH.md
        // R-101). PostConfigure, because the core's Configure replaces the validation parameters whatever the order.
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.ValidTypes = [JsonWebTokenTypes.AccessToken]);

        AddSignInSession(services, builder.Environment);
        AddIdentityServerOptions(services);

        // Expired, revoked and redeemed tokens and their authorizations would stay in the store forever
        // (docs/plan/RESEARCH.md R-088).
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ExpiredTokenPruner>();

        services.AddScoped<IPermissionResolver, IdentityPermissionResolver>();
        services.AddPermissionDefinitions<IdentityPermissionProvider>();

        // Every OpenAPI document declares the sign-in flow, so an API explorer can get a token from this provider.
        services.ConfigureAll<OpenApiOptions>(options => options.AddDocumentTransformer<OAuthSecuritySchemeTransformer>());

        return builder;
    }

    private static void AddAccountStore(IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                // Length first, no character-composition rules (OWASP Authentication Cheat Sheet); 15 characters for a
                // password that can be the only factor (NIST SP 800-63B-4). CommonPasswordValidator adds the blocklist.
                options.Password.RequiredLength = 15;
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
            // Before the EF stores, which add their user store only when none is registered.
            .AddUserStore<HashedRecoveryCodesUserStore>()
            .AddEntityFrameworkStores<IdentityServerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddTokenProvider<SingleUseAuthenticatorTokenProvider>(TokenOptions.DefaultAuthenticatorProvider);

        // NIST SP 800-63B-4: refuse commonly used passwords. Checked against a list in the package, never an outside
        // service. Next to Identity's own validator, which every password still passes first.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPasswordValidator<ApplicationUser>, CommonPasswordValidator>());

        // OWASP Password Storage Cheat Sheet: at least 600,000 iterations for PBKDF2-HMAC-SHA256.
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);

        // Email confirmation tokens expire after six hours.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(6));

        // Those tokens, the sign-in cookies and antiforgery are protected with Data Protection keys. The framework's
        // fallback store is the user profile or the process memory: lost when a container is replaced, different on every
        // instance. Where the keys live is the host's call (volume, database, blob store), so outside Development it must
        // make one (docs/plan/RESEARCH.md R-081). Every PersistKeysTo* sets XmlRepository.
        services.AddOptions<KeyManagementOptions>()
            .Validate<IHostEnvironment>(
                (keys, environment) => environment.IsDevelopment() || keys.XmlRepository is not null,
                "No Data Protection key store is configured. Outside Development the host must choose one, so the "
                + "identity provider's sign-in cookies, antiforgery tokens and email confirmation links survive restarts "
                + "and work on every instance: e.g. services.AddDataProtection().PersistKeysToFileSystem(<a persistent "
                + "volume>), .PersistKeysToDbContext<TContext>() (Microsoft.AspNetCore.DataProtection.EntityFrameworkCore) "
                + "or a blob or Redis store.")
            .ValidateOnStart();
    }

    private static void AddEmail(IServiceCollection services, IHostEnvironment environment)
    {
        // LoggingEmailSender writes confirmation links to the log: Development only, and only when the host brings none.
        // Anywhere else the host registers a real sender, or EmailSenderStartupCheck stops the host.
        if (environment.IsDevelopment())
            services.TryAddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<EmailSenderStartupCheck>();

        // The endpoints queue their emails and answer at once; EmailQueue sends them through the host's IEmailSender in
        // the background, so the response time doesn't reveal whether an address has an account (R-105).
        services.AddSingleton<EmailQueue>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailQueue>());
    }

    private static void AddOpenIddictServer(IServiceCollection services, IHostEnvironment environment)
    {
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

                // Seven days from the sign-in, not from the last refresh: by default each new refresh token gets a full
                // lifetime, so one used before every expiry never ran out (measured, PLAN F6b.4e; R-103).
                server.DisableSlidingRefreshTokenExpiration();

                if (environment.IsDevelopment())
                    server.AddEphemeralEncryptionKey().AddEphemeralSigningKey();

                server.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });
    }

    private static void AddSignInSession(IServiceCollection services, IHostEnvironment environment)
    {
        // The sign-in session at the provider: Identity's cookies, alongside the API's bearer default (the parameterless
        // AddAuthentication doesn't change the default scheme, docs/plan/RESEARCH.md R-061). Only /connect/* and
        // /account/* read it.
        services.AddAuthentication().AddIdentityCookies(cookies =>
        {
            cookies.ApplicationCookie!.Configure(cookie =>
            {
                cookie.LoginPath = LoginPage.Path;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                cookie.SlidingExpiration = true;

                // Only a sign-in raises this event, not a renewal, so the time stays the sign-in's for auth_time and max_age.
                cookie.Events.OnSigningIn = context =>
                {
                    ConnectEndpoints.RecordAuthenticationTime(context.Properties);
                    return Task.CompletedTask;
                };
            });

            // The other sign-in cookies too: by default they follow the request's scheme, which behind a TLS proxy without
            // forwarded headers is http (docs/plan/RESEARCH.md R-104). OpenIddict needs https anyway.
            cookies.TwoFactorUserIdCookie!.Configure(cookie => cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always);
            cookies.TwoFactorRememberMeCookie!.Configure(cookie => cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always);
        });

        // Antiforgery's cookie has no Secure flag by default, not even over https. Microsoft's guidance: Always outside
        // Development (R-104). In Development it follows the request, since Always fails a plain-http page outright.
        services.AddAntiforgery(antiforgery => antiforgery.Cookie.SecurePolicy =
            environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);

        // The cookie is read only at /connect/* and /account/*, so checking its security stamp on every read is cheap, and
        // a password change or "sign out everywhere" ends the provider session at once instead of within 30 minutes.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
    }

    private static void AddIdentityServerOptions(IServiceCollection services)
    {
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
            .Validate<IHostEnvironment>(
                (identity, environment) => environment.IsDevelopment() || identity.Clients.TrueForAll(client =>
                    client.RedirectUris.Concat(client.PostLogoutRedirectUris).All(IsSecureRedirect)),
                "Identity:Clients:n:RedirectUris and PostLogoutRedirectUris must use https outside Development; plain http "
                + "only on a loopback IP address (127.0.0.1, [::1]) for a native app (RFC 8252 §7.3).")
            // RFC 6749 §10.10: a guessable secret is none. 32 characters hold 128 bits or more when they are generated.
            .Validate(
                identity => identity.Clients.TrueForAll(client =>
                    string.IsNullOrEmpty(client.ClientSecret) || client.ClientSecret.Length >= IdentityClientOptions.MinimumSecretLength),
                "Identity:Clients:n:ClientSecret must be at least 32 characters: generate it, e.g. 32 random bytes in base64.")
            .Validate(
                identity => identity.Clients.TrueForAll(client => client.SignsInUsers || !string.IsNullOrEmpty(client.ClientSecret)),
                "Identity:Clients:n without RedirectUris is a service client (client credentials) and needs a ClientSecret.")
            .Validate(
                identity => identity.Clients.TrueForAll(client => !client.SignsInUsers || client.Permissions.Count == 0),
                "Identity:Clients:n:Permissions apply to service clients only: a client with RedirectUris acts for its users, "
                + "who get their own permissions.")
            // Every registered account gets the User role, so a typo would grant nothing and an admin permission would
            // make every self-registered account an administrator of accounts.
            .Validate<IEnumerable<IPermissionDefinitionProvider>>(
                (identity, providers) => identity.Seed.UserPermissions.TrueForAll(
                    providers.SelectMany(provider => provider.GetPermissions()).Select(permission => permission.Name).Contains),
                "Identity:Seed:UserPermissions must list permissions a module declares.")
            .Validate(
                identity => !identity.Seed.UserPermissions.Exists(
                    permission => permission.StartsWith(IdentityPermissions.Prefix, StringComparison.Ordinal)),
                "Identity:Seed:UserPermissions can't hold the identity provider's own permissions (identity:*): every "
                + "registered account gets them.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<IdentityServerOptions>, IdentityServerOptionsValidator>());
        services.AddHostedService<IdentityServerSeeder>();
    }

    // An authorization code sent over plain http can be read on the way, except to a native app's own loopback listener,
    // named by its IP address: "localhost" may resolve to another interface (RFC 8252 §7.3, §8.3). A relative URI is
    // the absolute-URI rule's to report.
    private static bool IsSecureRedirect(Uri uri) =>
        !uri.IsAbsoluteUri
        || !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
        || (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            && IPAddress.TryParse(uri.DnsSafeHost, out var address)
            && IPAddress.IsLoopback(address));

    /// <summary>
    /// Maps the provider's endpoints: the authorization, token and end-session endpoints under <c>/connect</c> (OpenIddict
    /// validates the requests first) and the browser pages under <c>/account</c> (sign-in at <c>/account/login</c>,
    /// password reset at <c>/account/forgot-password</c>), which stay out of the OpenAPI document; and the account API
    /// under <c>/api/v1/auth</c> (registration, email confirmation, the current user, password change, sign-out
    /// everywhere, two-factor authentication) and the user and role administration under <c>/api/v1/admin</c>, guarded
    /// by the <c>identity:*</c> permissions. The token endpoint, the pages and the account API use the <c>strict</c> rate
    /// limit.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapCoreIdentityServer(this IEndpointRouteBuilder endpoints)
    {
        ConnectEndpoints.Map(endpoints);

        // The pages a signed-out browser uses; the handlers read the sign-in session themselves. No cross-origin access:
        // the core's policy allows credentials for configured origins, and a script there mustn't read the pages with
        // the user's cookies (RFC 9700 §2.6, docs/plan/RESEARCH.md R-102).
        var account = endpoints.MapGroup("/account").ExcludeFromDescription().AllowAnonymous().RequireRateLimiting("strict")
            .WithMetadata(new DisableCorsAttribute());
        LoginPage.Map(account);
        PasswordResetPage.Map(account);

        AccountEndpoints.Map(endpoints);
        AdminEndpoints.Map(endpoints);
        return endpoints;
    }
}
