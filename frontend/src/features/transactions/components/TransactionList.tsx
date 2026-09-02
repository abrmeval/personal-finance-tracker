import { Pencil, Trash2, ArrowDownCircle, ArrowUpCircle } from "lucide-react";
import type { Transaction } from "@/types/finance";

interface TransactionListProps {
  transactions: Transaction[];
  isLoading: boolean;
  error: Error | null;
  onEdit: (transaction: Transaction) => void;
  onDelete: (transaction: Transaction) => void;
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("es-MX", {
    style: "currency",
    currency: "MXN",
  }).format(amount);
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString("es-MX", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function TransactionList({
  transactions,
  isLoading,
  error,
  onEdit,
  onDelete,
}: TransactionListProps) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="h-20 rounded-lg bg-gray-100 animate-pulse" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
        Failed to load transactions. Please try again.
      </div>
    );
  }

  if (transactions.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500 text-sm">
        No transactions found. Create your first transaction to start tracking
        your finances.
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {transactions.map((transaction) => (
        <div
          key={transaction.id}
          className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-4 py-3"
        >
          <div className="flex items-center gap-3 min-w-0 flex-1">
            {transaction.type === "Income" ? (
              <ArrowUpCircle className="h-5 w-5 flex-shrink-0 text-green-600" />
            ) : (
              <ArrowDownCircle className="h-5 w-5 flex-shrink-0 text-red-600" />
            )}
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-gray-900">
                {transaction.description}
              </p>
              <p className="text-xs text-gray-500">
                {formatDate(transaction.date)}
                {transaction.categoryName && ` · ${transaction.categoryName}`}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-3 flex-shrink-0">
            <span
              className={`text-sm font-semibold ${
                transaction.type === "Income"
                  ? "text-green-600"
                  : "text-red-600"
              }`}
            >
              {transaction.type === "Income" ? "+" : "-"}
              {formatCurrency(transaction.amount)}
            </span>
            <button
              onClick={() => onEdit(transaction)}
              className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
              aria-label={`Edit ${transaction.description}`}
            >
              <Pencil className="h-4 w-4" />
            </button>
            <button
              onClick={() => onDelete(transaction)}
              className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
              aria-label={`Delete ${transaction.description}`}
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
