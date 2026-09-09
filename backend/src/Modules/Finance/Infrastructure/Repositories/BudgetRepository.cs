using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Infrastructure.Data;

namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

public sealed class BudgetRepository(FinanceDbContext context) : IBudgetRepository
{
    public async Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Budgets
            .Where(b => b.UserId == userId && b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

    public async Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.IsActive, ct);

    public async Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
        => await context.Budgets
            .AnyAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.IsActive, ct);

    public async Task AddAsync(Budget budget, CancellationToken ct = default)
        => await context.Budgets.AddAsync(budget, ct);

    public Task DeleteAsync(Budget budget, CancellationToken ct = default)
    {
        budget.Deactivate();
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
