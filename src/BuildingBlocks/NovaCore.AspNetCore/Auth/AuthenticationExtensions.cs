using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.AspNetCore.Auth;

/// <summary>Service registration for JWT bearer authentication.</summary>
public static class AuthenticationExtensions
{
    private const string DevelopmentSigningKey = "novacore-development-only-signing-key-do-not-use-in-production-0123456789";

    // What an OpenID Connect provider signs with. Pinning the list blocks "alg" confusion and "none"; HS256 joins it only
    // while the Development-only symmetric key is configured.
    private static readonly string[] AsymmetricAlgorithms =
    [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
    ];

    /// <summary>
    /// Configures JWT bearer authentication from the <c>Jwt</c> config section, with strict token validation. Tokens are
    /// validated against <c>Jwt:Authority</c>: an OpenID Connect provider whose issuer and signing keys (JWKS) are
    /// discovered from its metadata. A symmetric <c>Jwt:SigningKey</c> is accepted in Development only; in Development a
    /// throwaway key is used when neither is configured, so the app runs out of the box. The options are validated when
    /// the host starts: outside Development <c>Jwt:Authority</c> (https) is required and <c>Jwt:SigningKey</c> is refused.
    /// </summary>
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .PostConfigure<IHostEnvironment>((jwt, environment) =>
            {
                if (environment.IsDevelopment()
                    && string.IsNullOrWhiteSpace(jwt.Authority)
                    && string.IsNullOrWhiteSpace(jwt.SigningKey))
                {
                    jwt.SigningKey = DevelopmentSigningKey;
                }
            })
            .Validate(
                jwt => string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) >= 32,
                "Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256.")
            // Asymmetric keys only outside Development (Microsoft's JWT guidance; docs/plan/RESEARCH.md R-056).
            .Validate<IHostEnvironment>(
                (jwt, environment) => environment.IsDevelopment() || string.IsNullOrWhiteSpace(jwt.SigningKey),
                "Jwt:SigningKey (a symmetric HS256 key) is accepted in Development only. Outside Development set "
                + "Jwt:Authority to the OpenID Connect provider that signs the access tokens.")
            .Validate<IHostEnvironment>(
                (jwt, environment) => IsValidAuthority(jwt.Authority, environment),
                "Jwt:Authority must be the absolute URI of an OpenID Connect provider, e.g. "
                + "https://idp.example.com/realms/novacore. It is required and must use https outside Development.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, IHostEnvironment>((options, jwtOptions, environment) =>
            {
                var jwt = jwtOptions.Value;

                // Set only in Development (validated on start), or when a Development setup uses an authority alone.
                var developmentKey = string.IsNullOrWhiteSpace(jwt.SigningKey)
                    ? null
                    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

                options.MapInboundClaims = false;                       // keep raw JWT claim names (sub, name, role, permission)
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = false;                              // no need to keep the token in the auth properties

                // The provider's issuer and signing keys are read from {Authority}/.well-known/openid-configuration.
                if (!string.IsNullOrWhiteSpace(jwt.Authority))
                    options.Authority = jwt.Authority;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = developmentKey is null ? null : jwt.Issuer,   // the authority's issuer comes from its metadata
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = developmentKey,                  // added to the keys the authority publishes
                    ValidAlgorithms = developmentKey is null
                        ? AsymmetricAlgorithms
                        : [.. AsymmetricAlgorithms, SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),               // tight skew, not the 5-minute default
                    NameClaimType = NovaClaims.Name,
                    RoleClaimType = NovaClaims.Role
                };
            });

        return services;
    }

    // Empty is fine only in Development (the symmetric key stands in); plain http only in Development (a local provider).
    private static bool IsValidAuthority(string authority, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return environment.IsDevelopment();

        return Uri.TryCreate(authority, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && environment.IsDevelopment()));
    }
}
