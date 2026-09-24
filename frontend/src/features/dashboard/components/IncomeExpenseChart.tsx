import { format } from "date-fns";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useIncomeVsExpenses } from "@/features/dashboard/hooks/useDashboard";
import { formatCurrency } from "@/utils/formatters";

interface IncomeExpenseChartProps {
  months?: number;
}

interface ChartRow {
  label: string;
  Income: number;
  Expenses: number;
}

export function IncomeExpenseChart({ months = 6 }: IncomeExpenseChartProps) {
  const { data: response, isLoading, error } = useIncomeVsExpenses(months);

  if (isLoading) {
    return (
      <div
        className="h-96 animate-pulse rounded-lg bg-gray-100"
        aria-label="Loading income vs expenses chart"
      />
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
        Failed to load income vs expenses. Please try again.
      </div>
    );
  }

  const rows: ChartRow[] = (response?.data ?? []).map((month) => ({
    label: format(new Date(month.year, month.month - 1, 1), "MMM yy"),
    Income: month.income,
    Expenses: month.expenses,
  }));
  const hasTrendData = rows.some(
    (row) => row.Income !== 0 || row.Expenses !== 0,
  );

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-5">
      <h2 className="mb-4 text-lg font-semibold text-gray-900">
        Income vs Expenses
      </h2>
      {!hasTrendData ? (
        <p className="py-8 text-center text-sm text-gray-500">
          No data available yet. Add some transactions to see the trend.
        </p>
      ) : (
        <div
          className="h-80"
          role="img"
          aria-label={`Line chart of monthly income versus expenses over the last ${months} months`}
        >
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={rows}
              margin={{ top: 8, right: 16, bottom: 0, left: 8 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
              <XAxis dataKey="label" tick={{ fontSize: 12, fill: "#6B7280" }} />
              <YAxis
                width={96}
                tick={{ fontSize: 12, fill: "#6B7280" }}
                tickFormatter={(value: number) => formatCurrency(value)}
              />
              <Tooltip formatter={(value) => formatCurrency(Number(value ?? 0))} />
              <Legend />
              <Line
                type="monotone"
                dataKey="Income"
                stroke="#10B981"
                strokeWidth={2}
                dot={false}
              />
              <Line
                type="monotone"
                dataKey="Expenses"
                stroke="#EF4444"
                strokeWidth={2}
                dot={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
