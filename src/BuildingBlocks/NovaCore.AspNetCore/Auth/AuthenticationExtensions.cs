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
    // What an OpenID Connect provider signs with. Pinning the list blocks "alg" confusion, "none" and symmetric keys.
    private static readonly string[] AsymmetricAlgorithms =
    [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
    ];

    /// <summary>
    /// Configures JWT bearer authentication from the <c>Jwt</c> config section, with strict token validation. Tokens are
    /// validated against <c>Jwt:Authority</c>: an OpenID Connect provider whose issuer and signing keys (JWKS) are
    /// discovered from its metadata. Only asymmetric signatures are accepted. The options are validated when the host
    /// starts: <c>Jwt:Authority</c> is required in every environment and must use https outside Development.
    /// </summary>
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate<IHostEnvironment>(
                (jwt, environment) => IsValidAuthority(jwt.Authority, environment),
                "Jwt:Authority must be the absolute URI of an OpenID Connect provider, e.g. "
                + "https://idp.example.com/realms/novacore. It is required, and must use https outside Development.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>());

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, IHostEnvironment>((options, jwtOptions, environment) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;                       // keep raw JWT claim names (sub, name, role)
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = false;                              // no need to keep the token in the auth properties

                // The provider's issuer and signing keys are read from {Authority}/.well-known/openid-configuration.
                options.Authority = jwt.Authority;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,                              // the issuer comes from the provider's metadata
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = AsymmetricAlgorithms,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),               // tight skew, not the 5-minute default
                    NameClaimType = NovaClaims.Name,
                    RoleClaimType = NovaClaims.Role
                };
            });

        return services;
    }

    // Plain http only in Development (a local provider).
    private static bool IsValidAuthority(string authority, IHostEnvironment environment) =>
        Uri.TryCreate(authority, UriKind.Absolute, out var uri)
        && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) || (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) && environment.IsDevelopment()));
}
