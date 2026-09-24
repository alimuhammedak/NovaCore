using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Core.Infrastructure.Persistence;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Modules;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Features;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Modules.Payments;

/// <summary>
/// The Payments module — the "add a payments module" scenario from the design. Drop in the project,
/// implement <see cref="IModule"/>, reference it from the host. No central wiring file changes.
/// </summary>
public sealed class PaymentsModule : IModule
{
    public string Name => "Payments";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PaymentsDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Database"),
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", PaymentsDbContext.Schema);
                    sql.EnableRetryOnFailure();
                });

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddPermissionDefinitions<PaymentsPermissionProvider>();
        services.AddValidatorsFromAssembly(typeof(PaymentsModule).Assembly, includeInternalTypes: true);

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/payments")
            .WithTags("Payments");

        CreatePayment.Map(group);
        GetPayments.Map(group);

        return endpoints;
    }
}
