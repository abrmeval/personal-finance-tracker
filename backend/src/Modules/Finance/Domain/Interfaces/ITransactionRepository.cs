using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Models;

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

    /// <summary>
    /// Returns income and expense totals for a user within an optional date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<TransactionTypeTotals> GetTotalsByTypeAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns per-month income and expense totals for a user within a date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<IReadOnlyList<MonthlyTotals>> GetMonthlyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns expense totals grouped by category for a user within a date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<IReadOnlyList<CategoryExpenseTotal>> GetExpenseTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns income and expense totals grouped by user for a calendar month.
    /// Cross-user read contract consumed exclusively by the Reporting module's
    /// MonthlyReportJob background job (Sprint 4) — never exposed via an endpoint.
    /// </summary>
    Task<IReadOnlyList<UserMonthlyTotals>> GetUserMonthlyTotalsAsync(
        int year,
        int month,
        CancellationToken ct = default);
}
