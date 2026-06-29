using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

public sealed record TransactionQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public TransactionType? Type { get; init; }
}