namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record MonthlyTotals(int Year, int Month, decimal TotalIncome, decimal TotalExpenses);
