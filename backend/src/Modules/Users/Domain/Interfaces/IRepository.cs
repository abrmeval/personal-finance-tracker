namespace Personal.FinanceTracker.Users.Domain.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD operations on entities of type T.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}