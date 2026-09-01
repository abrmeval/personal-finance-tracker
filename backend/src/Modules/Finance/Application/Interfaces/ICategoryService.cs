using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Application.Interfaces;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Result<CategoryResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct = default);
    Task<Result<CategoryResponse>> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}