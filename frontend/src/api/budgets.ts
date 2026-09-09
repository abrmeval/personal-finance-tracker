import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";
import type {
  BudgetWithSpending,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from "@/types/finance";

export const budgetsApi = {
  getAll: (): Promise<ApiResponse<BudgetWithSpending[]>> =>
    apiClient.get<BudgetWithSpending[]>("/budgets"),

  getById: (id: string): Promise<ApiResponse<BudgetWithSpending>> =>
    apiClient.get<BudgetWithSpending>(`/budgets/${id}`),

  create: (data: CreateBudgetRequest): Promise<ApiResponse<BudgetWithSpending>> =>
    apiClient.post<BudgetWithSpending>("/budgets", data),

  update: (
    id: string,
    data: UpdateBudgetRequest,
  ): Promise<ApiResponse<BudgetWithSpending>> =>
    apiClient.put<BudgetWithSpending>(`/budgets/${id}`, data),

  delete: (id: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/budgets/${id}`),
};
