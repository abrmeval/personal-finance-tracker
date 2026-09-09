using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Application.Interfaces;

public interface IBudgetService
{
    Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Result<BudgetWithSpendingResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<Result<BudgetWithSpendingResponse>> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default);
    Task<Result<BudgetWithSpendingResponse>> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
