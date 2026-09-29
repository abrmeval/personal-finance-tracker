using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Models;
using Personal.FinanceTracker.Finance.Infrastructure.Data;

namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

public sealed class TransactionRepository(FinanceDbContext context) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(userId, startDate, endDate, categoryId, type);

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountByUserAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(userId, startDate, endDate, categoryId, type);
        return await query.CountAsync(ct);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.IsActive, ct);

    public async Task<Transaction?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.IsActive, ct);

    public async Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Transactions
            .AnyAsync(t => t.Id == id && t.UserId == userId && t.IsActive, ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await context.Transactions.AddAsync(transaction, ct);

    public Task DeleteAsync(Transaction transaction, CancellationToken ct = default)
    {
        transaction.Deactivate();
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task<decimal> GetTotalExpensesByCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId
                && t.CategoryId == categoryId
                && t.Type == TransactionType.Expense
                && t.IsActive
                && t.Date >= from
                && t.Date <= to)
            .SumAsync(t => t.Amount, ct);
    }

    public async Task<TransactionTypeTotals> GetTotalsByTypeAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var query = context.Transactions.Where(t => t.UserId == userId && t.IsActive);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        var totals = await query
            .GroupBy(t => 1)
            .Select(g => new TransactionTypeTotals(
                g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m))
            .SingleOrDefaultAsync(ct);

        return totals ?? new TransactionTypeTotals(0m, 0m);
    }

    public async Task<IReadOnlyList<MonthlyTotals>> GetMonthlyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var totals = await context.Transactions
            .Where(t => t.UserId == userId && t.IsActive && t.Date >= from && t.Date <= to)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalIncome = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                TotalExpenses = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m)
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToListAsync(ct);

        return totals
            .Select(m => new MonthlyTotals(m.Year, m.Month, m.TotalIncome, m.TotalExpenses))
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryExpenseTotal>> GetExpenseTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var totals = await context.Transactions
            .Where(t => t.UserId == userId
                && t.IsActive
                && t.Type == TransactionType.Expense
                && t.CategoryId != null
                && t.Date >= from
                && t.Date <= to)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        return totals
            .Select(x => new CategoryExpenseTotal(x.CategoryId, x.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<UserMonthlyTotals>> GetUserMonthlyTotalsAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1).AddTicks(-1);

        var totals = await context.Transactions
            .Where(t => t.IsActive && t.Date >= from && t.Date <= to)
            .GroupBy(t => t.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalIncome = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0m),
                TotalExpenses = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0m)
            })
            .ToListAsync(ct);

        return totals
            .Select(x => new UserMonthlyTotals(x.UserId, x.TotalIncome, x.TotalExpenses))
            .ToList();
    }

    private IQueryable<Transaction> BuildFilteredQuery(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type)
    {
        var query = context.Transactions.Where(t => t.UserId == userId && t.IsActive);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc));

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return query;
    }
}
