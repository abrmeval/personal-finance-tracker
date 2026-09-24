namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record CategoryBreakdownResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Total);
