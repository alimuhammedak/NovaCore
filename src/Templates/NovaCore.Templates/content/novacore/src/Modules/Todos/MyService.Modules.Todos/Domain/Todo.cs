using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Results;

namespace MyService.Modules.Todos.Domain;

/// <summary>A thing to do. Its state changes through methods; an expected failure is a <see cref="Result"/>.</summary>
public sealed class Todo : AuditableEntity<Guid>
{
    // EF Core materialization constructor.
    private Todo() { }

    public Todo(string title)
        : base(Guid.CreateVersion7())
    {
        Title = title;
    }

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
