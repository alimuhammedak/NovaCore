using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Core.Infrastructure.Persistence;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Modules;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Features;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Options;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity;

/// <summary>
/// The Identity module: ASP.NET Core Identity (hardened) + JWT/refresh tokens + 2FA + admin. Owns the
/// <c>identity</c> schema. Discovered and wired automatically like any other module.
/// </summary>
public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Database"),
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema);
                    sql.EnableRetryOnFailure();
                });
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        // Identity *core* only — no cookie schemes, because the API authenticates with JWT bearer.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                // OWASP: length first, no character-composition rules.
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Brute-force throttling at the account level.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Harden the PBKDF2 work factor (OWASP: ≥ 600,000 for PBKDF2-HMAC-SHA256).
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);

        // Tighten the lifetime of email-confirmation / reset tokens.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(6));

        services.AddPermissionDefinitions<IdentityPermissionProvider>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.AddScoped<IdentityDataSeeder>();
        services.AddHostedService<IdentitySeederHostedService>();

        services.AddValidatorsFromAssembly(typeof(IdentityModule).Assembly, includeInternalTypes: true);

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Auth endpoints get the stricter rate-limit policy (brute-force defence).
        var auth = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting("strict");

        RegistrationEndpoints.Map(auth);
        LoginEndpoints.Map(auth);
        SessionEndpoints.Map(auth);
        TwoFactorEndpoints.Map(auth);

        var admin = endpoints
            .MapGroup("/api/v1/admin")
            .WithTags("Admin");

        AdminEndpoints.Map(admin);

        return endpoints;
    }
}
