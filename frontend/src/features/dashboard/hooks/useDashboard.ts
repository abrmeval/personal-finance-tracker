import { useQuery } from "@tanstack/react-query";
import { reportsApi } from "@/api/reports";

export const dashboardKeys = {
  all: ["dashboard"] as const,
  summary: () => [...dashboardKeys.all, "summary"] as const,
  incomeVsExpenses: (months: number) =>
    [...dashboardKeys.all, "income-vs-expenses", months] as const,
  categoryBreakdown: (startDate: string, endDate: string) =>
    [...dashboardKeys.all, "category-breakdown", startDate, endDate] as const,
};

export function useDashboardSummary() {
  return useQuery({
    queryKey: dashboardKeys.summary(),
    queryFn: () => reportsApi.getDashboardSummary(),
    staleTime: 1000 * 60 * 2,
  });
}

export function useIncomeVsExpenses(months = 6) {
  return useQuery({
    queryKey: dashboardKeys.incomeVsExpenses(months),
    queryFn: () => reportsApi.getIncomeVsExpenses(months),
  });
}

export function useCategoryBreakdown(startDate: string, endDate: string) {
  return useQuery({
    queryKey: dashboardKeys.categoryBreakdown(startDate, endDate),
    queryFn: () => reportsApi.getCategoryBreakdown(startDate, endDate),
    enabled: startDate !== "" && endDate !== "",
  });
}
