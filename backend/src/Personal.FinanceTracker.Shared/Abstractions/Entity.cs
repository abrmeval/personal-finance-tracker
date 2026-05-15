namespace Personal.FinanceTracker.Shared.Abstractions;


/// <summary>
/// Base class for all entities in the system. Provides common properties like Id, CreatedAt, and UpdatedAt.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
}