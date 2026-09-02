import { useState } from "react";
import { Plus, X, ChevronLeft, ChevronRight } from "lucide-react";
import {
  useTransactions,
  useCreateTransaction,
  useUpdateTransaction,
  useDeleteTransaction,
} from "@/features/transactions/hooks/useTransactions";
import { TransactionForm } from "@/features/transactions/components/TransactionForm";
import { TransactionList } from "@/features/transactions/components/TransactionList";
import type { Transaction, TransactionFilters } from "@/types/finance";
import type { TransactionFormData } from "@/features/transactions/schemas";
import { setDocumentTitle } from "@/utils/documentTitle";

const DEFAULT_FILTERS: TransactionFilters = {
  page: 1,
  pageSize: 20,
};

export function TransactionsPage() {
  setDocumentTitle("Transactions");
  const [filters, setFilters] = useState<TransactionFilters>(DEFAULT_FILTERS);
  const { data: response, isLoading, error } = useTransactions(filters);
  const createMutation = useCreateTransaction();
  const updateMutation = useUpdateTransaction();
  const deleteMutation = useDeleteTransaction();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTransaction, setEditingTransaction] =
    useState<Transaction | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Transaction | null>(null);

  const pagedData = response?.data;
  const transactions = pagedData?.items ?? [];
  const totalPages = pagedData?.totalPages ?? 0;
  const currentPage = pagedData?.page ?? 1;

  function handleOpenCreate() {
    setEditingTransaction(null);
    setIsModalOpen(true);
  }

  function handleOpenEdit(transaction: Transaction) {
    setEditingTransaction(transaction);
    setIsModalOpen(true);
  }

  function handleCloseModal() {
    setIsModalOpen(false);
    setEditingTransaction(null);
  }

  async function handleSubmit(data: TransactionFormData) {
    const payload = {
      description: data.description,
      amount: data.amount,
      type: data.type,
      date: data.date,
      categoryId: data.categoryId || null,
      notes: data.notes || null,
    };

    if (editingTransaction) {
      await updateMutation.mutateAsync({
        id: editingTransaction.id,
        data: payload,
      });
    } else {
      await createMutation.mutateAsync(payload);
    }
    handleCloseModal();
  }

  async function handleConfirmDelete() {
    if (!deleteTarget) return;
    await deleteMutation.mutateAsync(deleteTarget.id);
    setDeleteTarget(null);
  }

  function handlePageChange(newPage: number) {
    setFilters((prev) => ({ ...prev, page: newPage }));
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Transactions</h1>
        <button
          onClick={handleOpenCreate}
          className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Add Transaction
        </button>
      </div>

      <TransactionList
        transactions={transactions}
        isLoading={isLoading}
        error={error}
        onEdit={handleOpenEdit}
        onDelete={setDeleteTarget}
      />

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-4">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage <= 1}
            className="rounded-md p-2 text-gray-400 hover:text-indigo-600 disabled:opacity-30 disabled:cursor-not-allowed"
            aria-label="Previous page"
          >
            <ChevronLeft className="h-5 w-5" />
          </button>
          <span className="text-sm text-gray-600">
            Page {currentPage} of {totalPages}
          </span>
          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage >= totalPages}
            className="rounded-md p-2 text-gray-400 hover:text-indigo-600 disabled:opacity-30 disabled:cursor-not-allowed"
            aria-label="Next page"
          >
            <ChevronRight className="h-5 w-5" />
          </button>
        </div>
      )}

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl max-h-[90dvh] overflow-y-auto">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900">
                {editingTransaction ? "Edit Transaction" : "New Transaction"}
              </h2>
              <button
                onClick={handleCloseModal}
                className="rounded-md p-1 text-gray-400 hover:text-gray-600"
                aria-label="Close"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            <TransactionForm
              defaultValues={
                editingTransaction
                  ? {
                      description: editingTransaction.description,
                      amount: editingTransaction.amount,
                      type: editingTransaction.type,
                      date: editingTransaction.date.split("T")[0],
                      categoryId: editingTransaction.categoryId ?? "",
                      notes: editingTransaction.notes ?? "",
                    }
                  : undefined
              }
              onSubmit={handleSubmit}
              isSubmitting={isSubmitting}
              submitLabel={
                editingTransaction ? "Update Transaction" : "Create Transaction"
              }
            />
          </div>
        </div>
      )}

      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
            <h2 className="text-lg font-semibold text-gray-900">
              Delete Transaction
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              Are you sure you want to delete "{deleteTarget.description}"?
            </p>
            <div className="mt-6 flex gap-3">
              <button
                onClick={() => setDeleteTarget(null)}
                className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmDelete}
                disabled={deleteMutation.isPending}
                className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
              >
                {deleteMutation.isPending ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
