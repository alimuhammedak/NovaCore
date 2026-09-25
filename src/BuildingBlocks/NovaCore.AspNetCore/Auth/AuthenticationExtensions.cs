using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.AspNetCore.Auth;

/// <summary>Service registration for JWT bearer authentication.</summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT bearer authentication from the <c>Jwt</c> config section, with strict token
    /// validation. In Development a throwaway signing key is generated if none is configured so the app
    /// runs out of the box; production MUST set <c>Jwt:SigningKey</c> (≥ 32 bytes).
    /// </summary>
    public static IServiceCollection AddCoreAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Jwt:SigningKey is required outside Development. Set it via configuration or user-secrets.");

            jwt.SigningKey = "novacore-development-only-signing-key-do-not-use-in-production-0123456789";
        }

        if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256.");

        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();

        var keyBytes = Encoding.UTF8.GetBytes(jwt.SigningKey);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
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
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
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
