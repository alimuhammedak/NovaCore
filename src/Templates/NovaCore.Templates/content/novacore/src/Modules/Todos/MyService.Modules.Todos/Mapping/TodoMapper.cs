using MyService.Modules.Todos.Domain;
using Riok.Mapperly.Abstractions;

namespace MyService.Modules.Todos.Mapping;

/// <summary>
/// Source-generated mapping (Mapperly): no reflection at run time. Every DTO member must be mapped; the entity's audit
/// fields are left out on purpose, so only the target side is required.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TodoMapper
{
    public static partial TodoDto ToDto(Todo todo);

    /// <summary>An EF-translatable projection: the list query selects only the DTO's columns.</summary>
    public static partial IQueryable<TodoDto> ProjectToDto(this IQueryable<Todo> source);
}
