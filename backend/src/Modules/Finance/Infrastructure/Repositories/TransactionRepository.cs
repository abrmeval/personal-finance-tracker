using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
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
        => await context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Transaction?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    public async Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Transactions
            .AnyAsync(t => t.Id == id && t.UserId == userId, ct);

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
                && t.Date >= from
                && t.Date <= to)
            .SumAsync(t => t.Amount, ct);
    }

    private IQueryable<Transaction> BuildFilteredQuery(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type)
    {
        var query = context.Transactions.Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return query;
    }
}