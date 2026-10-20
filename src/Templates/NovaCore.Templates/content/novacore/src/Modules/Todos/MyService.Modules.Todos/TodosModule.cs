using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Features;
using MyService.Modules.Todos.Infrastructure;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Modules;
using NovaCore.EntityFrameworkCore.Persistence;

namespace MyService.Modules.Todos;

/// <summary>
/// The Todos module, added by the host with <c>builder.AddModule&lt;TodosModule&gt;()</c>: registers its DbContext,
/// permissions and request validation, and maps its endpoints under <c>/api/v1/todos</c>.
/// </summary>
public sealed class TodosModule : IModule
{
    public string Name => "Todos";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration)
    {
        // Also adds the outbox interceptor and a "ready" health check for the context.
        services.AddModuleDbContext<TodosDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Database"),
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", TodosDbContext.Schema);
                    sql.EnableRetryOnFailure();
                });

            // Stamps CreatedBy/CreatedAtUtc/ModifiedBy/ModifiedAtUtc on every save.
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddPermissionDefinitions<TodosPermissionProvider>();

        // Built-in validation of the request records' DataAnnotations. The source generator only covers the assembly
        // that calls AddValidation, so each module calls it; without the call nothing is validated.
        services.AddValidation();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/todos")
            .WithTags("Todos");

        CreateTodo.Map(group);
        GetTodos.Map(group);
        GetTodoById.Map(group);
        CompleteTodo.Map(group);

        return endpoints;
    }
}
