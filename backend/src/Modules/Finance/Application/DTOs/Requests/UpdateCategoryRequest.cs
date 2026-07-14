namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Icon,
    string? Color);