namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record UserMonthlyTotals(Guid UserId, decimal TotalIncome, decimal TotalExpenses);
