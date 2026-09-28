using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Modules;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Features;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Modules.Payments;

/// <summary>
/// The Payments module — the "add a payments module" scenario from the design. Drop in the project,
/// implement <see cref="IModule"/>, reference it from the host and add it with <c>AddModule&lt;PaymentsModule&gt;()</c>.
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

        // Settles a payment when the Catalog module answers its PaymentCreated; the outbox delivers the answer.
        services.AddScoped<IDomainEventHandler<StockReserved>, SettlePayment.WhenStockReserved>();
        services.AddScoped<IDomainEventHandler<StockReservationFailed>, SettlePayment.WhenStockReservationFailed>();

        // Built-in validation: generated per assembly, so each module calls it itself (docs/plan/RESEARCH.md R-052).
        services.AddValidation();

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
