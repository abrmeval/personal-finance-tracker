import { Pencil, Trash2, AlertTriangle } from "lucide-react";
import type { BudgetWithSpending } from "@/types/finance";
import { formatCurrency } from "@/utils/formatters";

interface BudgetCardProps {
  budget: BudgetWithSpending;
  onEdit: (budget: BudgetWithSpending) => void;
  onDelete: (budget: BudgetWithSpending) => void;
}

function getProgressColor(percentageUsed: number): string {
  if (percentageUsed >= 100) return "bg-red-600";
  if (percentageUsed >= 75) return "bg-amber-500";
  return "bg-green-600";
}

export function BudgetCard({ budget, onEdit, onDelete }: BudgetCardProps) {
  const progressWidth = Math.min(budget.percentageUsed, 100);

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-3">
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <p className="truncate text-sm font-medium text-gray-900">{budget.name}</p>
            {budget.isOverBudget && (
              <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-700">
                <AlertTriangle className="h-3 w-3" aria-hidden="true" />
                Over budget
              </span>
            )}
          </div>
          <p className="text-xs text-gray-500">
            {budget.categoryName} · {budget.period}
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          <div className="text-right">
            <p className="text-sm font-semibold text-gray-900">
              {formatCurrency(budget.spentAmount)} / {formatCurrency(budget.limitAmount)}
            </p>
            <p className={`text-xs font-medium ${budget.isOverBudget ? "text-red-600" : "text-gray-500"}`}>
              {budget.percentageUsed}% used
            </p>
          </div>
          <button
            onClick={() => onEdit(budget)}
            className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
            aria-label={`Edit ${budget.name}`}
          >
            <Pencil className="h-4 w-4" />
          </button>
          <button
            onClick={() => onDelete(budget)}
            className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
            aria-label={`Delete ${budget.name}`}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div
        className="mt-3 h-2 w-full overflow-hidden rounded-full bg-gray-100"
        role="progressbar"
        aria-valuenow={budget.percentageUsed}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${budget.name} budget usage`}
      >
        <div
          className={`h-full rounded-full transition-colors ${getProgressColor(budget.percentageUsed)}`}
          style={{ width: `${progressWidth}%` }}
        />
      </div>
    </div>
  );
}
