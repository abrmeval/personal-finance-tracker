import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";
import type {
  CategoryBreakdown,
  DashboardSummary,
  MonthlyTotals,
} from "@/types/reporting";

export const reportsApi = {
  getDashboardSummary: (): Promise<ApiResponse<DashboardSummary>> =>
    apiClient.get<DashboardSummary>("/reports/dashboard/summary"),

  getIncomeVsExpenses: (months: number): Promise<ApiResponse<MonthlyTotals[]>> =>
    apiClient.get<MonthlyTotals[]>(
      `/reports/dashboard/income-vs-expenses?months=${encodeURIComponent(String(months))}`,
    ),

  getCategoryBreakdown: (
    startDate: string,
    endDate: string,
  ): Promise<ApiResponse<CategoryBreakdown[]>> =>
    apiClient.get<CategoryBreakdown[]>(
      `/reports/dashboard/category-breakdown?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`,
    ),
};
