import { endOfMonth, format, parseISO, startOfMonth } from "date-fns";
import { useEffect, useState } from "react";
import { IncomeExpenseChart } from "@/features/dashboard/components/IncomeExpenseChart";
import { SpendingPieChart } from "@/features/dashboard/components/SpendingPieChart";
import type { DateRange } from "@/utils/dates";
import { setDocumentTitle } from "@/utils/documentTitle";

function getMonthRange(monthValue: string): DateRange {
  const monthStart = startOfMonth(parseISO(`${monthValue}-01`));
  return {
    startDate: format(monthStart, "yyyy-MM-dd"),
    endDate: format(endOfMonth(monthStart), "yyyy-MM-dd"),
  };
}

export function ReportsPage() {
  useEffect(() => {
    setDocumentTitle("Reports");
  }, []);

  const [month, setMonth] = useState(() => format(new Date(), "yyyy-MM"));
  const { startDate, endDate } = getMonthRange(month);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Reports</h1>
        <div>
          <label
            htmlFor="report-month"
            className="mb-1 block text-sm font-medium text-gray-700"
          >
            Month
          </label>
          <input
            id="report-month"
            type="month"
            value={month}
            min="2000-01"
            onChange={(event) => {
              if (event.target.value) setMonth(event.target.value);
            }}
            className="min-h-11 rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
          />
        </div>
      </div>

      <SpendingPieChart startDate={startDate} endDate={endDate} />
      <IncomeExpenseChart months={12} />
    </div>
  );
}
