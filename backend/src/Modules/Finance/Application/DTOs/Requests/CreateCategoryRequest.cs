namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

public sealed record CreateCategoryRequest(
    string Name,
    string? Icon,
    string? Color);