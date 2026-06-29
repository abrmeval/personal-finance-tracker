using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

public sealed record CreateTransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    Guid? CategoryId,
    string? Notes);