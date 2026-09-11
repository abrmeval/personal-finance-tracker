using Personal.FinanceTracker.Finance.Domain.Entities;

namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default);
    Task AddAsync(Budget budget, CancellationToken ct = default);
    Task DeleteAsync(Budget budget, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
