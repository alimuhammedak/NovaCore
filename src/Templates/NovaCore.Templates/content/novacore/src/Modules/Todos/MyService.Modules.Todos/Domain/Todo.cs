using NovaCore.Abstractions.Entities;
#if (multitenancy)
using NovaCore.Abstractions.MultiTenancy;
#endif
using NovaCore.Abstractions.Results;

namespace MyService.Modules.Todos.Domain;

#if (multitenancy)
/// <summary>
/// A thing to do, in one tenant. Its state changes through methods; an expected failure is a <see cref="Result"/>. As an
/// <see cref="ITenantEntity"/>, the module's context keeps it in its tenant: queries see the request's tenant's todos
/// only, and a new todo is saved in that tenant.
/// </summary>
public sealed class Todo : AuditableEntity<Guid>, ITenantEntity
#else
/// <summary>A thing to do. Its state changes through methods; an expected failure is a <see cref="Result"/>.</summary>
public sealed class Todo : AuditableEntity<Guid>
#endif
{
    // EF Core materialization constructor.
    private Todo() { }

    public Todo(string title)
        : base(Guid.CreateVersion7())
    {
        Title = title;
    }

#if (multitenancy)
    /// <summary>The todo's tenant. Left empty on a new todo: saving it fills in the request's tenant.</summary>
    public Guid TenantId { get; set; }

#endif
    public string Title { get; private set; } = string.Empty;

    public bool IsCompleted { get; private set; }

    /// <summary>Marks the todo completed. A completed todo can't be completed again.</summary>
    public Result Complete()
    {
        if (IsCompleted)
            return Result.Failure(Error.Conflict("todo.already_completed", $"Todo '{Title}' is already completed."));

        IsCompleted = true;
        return Result.Success();
    }
}
