namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record MonthlyTotalsResponse(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses);
