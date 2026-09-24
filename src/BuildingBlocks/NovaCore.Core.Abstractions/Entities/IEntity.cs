namespace NovaCore.Core.Abstractions.Entities;

/// <summary>Marker for a persistable entity.</summary>
public interface IEntity;

/// <summary>An entity with a strongly-typed identity.</summary>
public interface IEntity<out TId> : IEntity
{
    TId Id { get; }
}
