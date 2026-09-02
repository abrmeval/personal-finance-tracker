using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        CancellationToken ct = default);

    Task<int> CountByUserAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        CancellationToken ct = default);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Transaction?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Calculates the total expense amount for a user's category within a date range.
    /// Used by BudgetService (Sprint 3) to compute spending against budget limits.
    /// </summary>
    Task<decimal> GetTotalExpensesByCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}