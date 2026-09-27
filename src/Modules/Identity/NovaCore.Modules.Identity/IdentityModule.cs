using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Modules;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Features;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Options;
using NovaCore.Modules.Identity.Services;

namespace NovaCore.Modules.Identity;

/// <summary>
/// The Identity module: ASP.NET Core Identity (hardened) + JWT/refresh tokens + 2FA + admin. Owns the
/// <c>identity</c> schema. Added by the host with <c>AddModule&lt;IdentityModule&gt;()</c> like any other module.
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
        services.AddScoped<IPermissionResolver, IdentityPermissionResolver>(); // the core resolves permissions through it
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();

        // LoggingEmailSender writes confirmation links (tokens) to the log: Development only. Elsewhere the host must
        // register a real IEmailSender, or EmailSenderStartupCheck stops startup.
        if (IsDevelopment(configuration))
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<EmailSenderStartupCheck>();

        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.AddScoped<IdentityDataSeeder>();
        services.AddHostedService<IdentitySeederHostedService>();

        // Built-in validation: generated per assembly, so each module calls it itself (docs/plan/RESEARCH.md R-052).
        services.AddValidation();

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

    // AddModule gets no IHostEnvironment. The host builds IHostEnvironment.EnvironmentName from this same configuration
    // key; when it's unset the environment is Production (docs/plan/RESEARCH.md R-030).
    private static bool IsDevelopment(IConfiguration configuration) =>
        string.Equals(configuration[HostDefaults.EnvironmentKey], Environments.Development, StringComparison.OrdinalIgnoreCase);
}
