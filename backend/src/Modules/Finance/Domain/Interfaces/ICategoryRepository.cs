using Personal.FinanceTracker.Finance.Domain.Entities;

namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Category?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default);
    Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}