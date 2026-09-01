using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Infrastructure.Data;

namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

public sealed class CategoryRepository(FinanceDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Categories
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.IsActive, ct);

    public async Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default)
        => await context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name == name && c.IsActive, ct);

    public async Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
        => await context.Categories
            .AnyAsync(c => c.Id == id && c.UserId == userId && c.IsActive, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
        => await context.Categories.AddAsync(category, ct);

    public Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        category.Deactivate();
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}