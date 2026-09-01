using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Application.Interfaces;

public interface ITransactionService
{
    Task<Result<PagedResult<TransactionResponse>>> GetAllAsync(Guid userId, TransactionQueryParams queryParams, CancellationToken ct = default);
    Task<Result<TransactionResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken ct = default);
    Task<Result<TransactionResponse>> UpdateAsync(Guid userId, Guid id, UpdateTransactionRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}