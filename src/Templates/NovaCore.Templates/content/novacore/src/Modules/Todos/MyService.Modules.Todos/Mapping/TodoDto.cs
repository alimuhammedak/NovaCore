namespace MyService.Modules.Todos.Mapping;

/// <summary>A todo as the API returns it. Entities never leave the module through an endpoint.</summary>
public sealed record TodoDto(Guid Id, string Title, bool IsCompleted);
