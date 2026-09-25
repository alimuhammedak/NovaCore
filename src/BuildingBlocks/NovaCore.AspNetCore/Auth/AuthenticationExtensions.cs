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

    /// <summary>
    /// Configures JWT bearer authentication from the <c>Jwt</c> config section, with strict token validation. The
    /// options are validated when the host starts. In Development a throwaway signing key is used if none is configured
    /// so the app runs out of the box; every other environment MUST set <c>Jwt:SigningKey</c> (≥ 32 bytes).
    /// </summary>
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .PostConfigure<IHostEnvironment>((jwt, environment) =>
            {
                if (string.IsNullOrWhiteSpace(jwt.SigningKey) && environment.IsDevelopment())
                    jwt.SigningKey = DevelopmentSigningKey;
            })
            // A missing key is the [Required] rule's message; this one covers a key that is present but too short.
            .Validate(
                jwt => string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) >= 32,
                "Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256.")
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

                options.MapInboundClaims = false;                       // keep raw JWT claim names (sub, name, role, permission)
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = false;                              // no need to keep the token in the auth properties
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],  // pin the algorithm — block "alg" downgrade/confusion
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),               // tight skew, not the 5-minute default
                    NameClaimType = NovaClaims.Name,
                    RoleClaimType = NovaClaims.Role
                };
            });

        return services;
    }
}
