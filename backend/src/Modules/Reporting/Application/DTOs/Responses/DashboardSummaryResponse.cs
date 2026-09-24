namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record DashboardSummaryResponse(
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses);
