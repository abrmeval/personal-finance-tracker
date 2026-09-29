import {
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from "recharts";
import { useCategoryBreakdown } from "@/features/dashboard/hooks/useDashboard";
import { formatCurrency } from "@/utils/formatters";

const COLORS = [
  "#3B82F6",
  "#EF4444",
  "#10B981",
  "#F59E0B",
  "#8B5CF6",
  "#EC4899",
  "#06B6D4",
  "#F97316",
];

interface SpendingPieChartProps {
  startDate: string;
  endDate: string;
}

interface ChartSlice {
  id: string;
  name: string;
  value: number;
}

export function SpendingPieChart({ startDate, endDate }: SpendingPieChartProps) {
  const { data: response, isLoading, error } = useCategoryBreakdown(
    startDate,
    endDate,
  );

  if (isLoading) {
    return (
      <div
        className="h-96 animate-pulse rounded-lg bg-gray-100"
        aria-label="Loading spending by category chart"
      />
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
        Failed to load spending by category. Please try again.
      </div>
    );
  }

  const slices: ChartSlice[] = (response?.data ?? []).map((category) => ({
    id: category.categoryId,
    name: category.categoryName,
    value: category.total,
  }));

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-5">
      <h2 className="mb-4 text-lg font-semibold text-gray-900">
        Spending by Category
      </h2>
      {slices.length === 0 ? (
        <p className="py-8 text-center text-sm text-gray-500">
          No spending data for this period.
        </p>
      ) : (
        <div
          className="h-80"
          role="img"
          aria-label="Pie chart of expenses by category for the selected period"
        >
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={slices}
                dataKey="value"
                nameKey="name"
                innerRadius={60}
                outerRadius={95}
                paddingAngle={2}
              >
                {slices.map((slice, index) => (
                  <Cell key={slice.id} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip formatter={(value) => formatCurrency(Number(value ?? 0))} />
              <Legend
                verticalAlign="bottom"
                height={36}
                formatter={(value) => (
                  <span className="text-xs text-gray-600">{value}</span>
                )}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
