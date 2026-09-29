import { useEffect } from "react";
import { IncomeExpenseChart } from "@/features/dashboard/components/IncomeExpenseChart";
import { OverviewCards } from "@/features/dashboard/components/OverviewCards";
import { SpendingPieChart } from "@/features/dashboard/components/SpendingPieChart";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboard";
import { getCurrentMonthRange } from "@/utils/dates";
import { setDocumentTitle } from "@/utils/documentTitle";

export function DashboardPage() {
  useEffect(() => {
    setDocumentTitle("Dashboard");
  }, []);

  const { data: summaryResponse, isLoading, error } = useDashboardSummary();
  const { startDate, endDate } = getCurrentMonthRange();

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          Failed to load dashboard summary. Please try again.
        </div>
      )}

      <OverviewCards
        summary={summaryResponse?.data ?? null}
        isLoading={isLoading}
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <SpendingPieChart startDate={startDate} endDate={endDate} />
        <IncomeExpenseChart months={6} />
      </div>
    </div>
  );
}
