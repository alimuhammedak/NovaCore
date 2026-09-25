namespace NovaCore.Core.Abstractions.Entities;

/// <summary>Marker for a persistable entity.</summary>
public interface IEntity;

/// <summary>An entity with a strongly-typed identity.</summary>
/// <typeparam name="TId">The type of the identity.</typeparam>
public interface IEntity<out TId> : IEntity
{
    /// <summary>The entity's identity.</summary>
    TId Id { get; }
}
