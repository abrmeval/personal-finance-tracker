import type { BudgetWithSpending } from "@/types/finance";
import { BudgetCard } from "@/features/budgets/components/BudgetCard";

interface BudgetListProps {
  budgets: BudgetWithSpending[];
  isLoading: boolean;
  error: Error | null;
  onEdit: (budget: BudgetWithSpending) => void;
  onDelete: (budget: BudgetWithSpending) => void;
}

export function BudgetList({ budgets, isLoading, error, onEdit, onDelete }: BudgetListProps) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-24 rounded-lg bg-gray-100 animate-pulse" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
        Failed to load budgets. Please try again.
      </div>
    );
  }

  if (budgets.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500 text-sm">
        No budgets yet. Create your first budget to start tracking spending.
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {budgets.map((budget) => (
        <BudgetCard key={budget.id} budget={budget} onEdit={onEdit} onDelete={onDelete} />
      ))}
    </div>
  );
}
