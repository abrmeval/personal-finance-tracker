namespace Personal.FinanceTracker.Reporting.Application.DTOs.Requests;

public sealed record CategoryBreakdownQueryParams(
    DateTime? StartDate,
    DateTime? EndDate);
