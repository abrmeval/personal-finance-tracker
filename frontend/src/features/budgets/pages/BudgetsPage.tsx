import { useEffect, useState } from "react";
import { Plus, X } from "lucide-react";
import {
  useBudgets,
  useCreateBudget,
  useUpdateBudget,
  useDeleteBudget,
} from "@/features/budgets/hooks/useBudgets";
import { BudgetForm } from "@/features/budgets/components/BudgetForm";
import { BudgetList } from "@/features/budgets/components/BudgetList";
import type { BudgetWithSpending } from "@/types/finance";
import type { BudgetFormData } from "@/features/budgets/schemas";
import { setDocumentTitle } from "@/utils/documentTitle";
import { ApiError, AppStatusCode } from "@/types/http";
import { ClientLogger, type ClientLogEntry } from "@/utils/clientLogger";

export function BudgetsPage() {
  useEffect(() => {
    setDocumentTitle("Budgets");
  }, []);

  const { data: response, isLoading, error } = useBudgets();
  const createMutation = useCreateBudget();
  const updateMutation = useUpdateBudget();
  const deleteMutation = useDeleteBudget();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingBudget, setEditingBudget] = useState<BudgetWithSpending | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<BudgetWithSpending | null>(null);
  const [errorDetails, setErrorDetails] = useState<string | null>(null);
  const [modelErrors, setModelErrors] = useState<Record<
    string,
    string[]
  > | null>(null);

  const budgets = response?.data ?? [];

  function handleOpenCreate() {
    setEditingBudget(null);
    setIsModalOpen(true);
  }

  function handleOpenEdit(budget: BudgetWithSpending) {
    setEditingBudget(budget);
    setIsModalOpen(true);
  }

  function handleCloseModal() {
    setIsModalOpen(false);
    setEditingBudget(null);
    setErrorDetails(null);
    setModelErrors(null);
  }

  function handleCloseDelete() {
    setDeleteTarget(null);
    setErrorDetails(null);
    setModelErrors(null);
  }

  async function handleSubmit(data: BudgetFormData) {
    setErrorDetails(null);
    setModelErrors(null);

    try {
      if (editingBudget) {
        await updateMutation.mutateAsync({
          id: editingBudget.id,
          data: {
            categoryId: data.categoryId,
            name: data.name,
            limitAmount: data.limitAmount,
            period: data.period,
          },
        });
      } else {
        await createMutation.mutateAsync({
          categoryId: data.categoryId,
          name: data.name,
          limitAmount: data.limitAmount,
          period: data.period,
        });
      }
      handleCloseModal();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorDetails(error.detail);

        if (error.modelErrors) {
          setModelErrors(error.modelErrors);
        }
      } else {
        ClientLogger.LogError({
          message: "Unexpected error while submitting the request",
          details: error instanceof Error ? error.message : String(error),
          context: "[onSubmit]",
          path: "/budgets",
          statusCode: AppStatusCode.ClientError,
        } as ClientLogEntry);
        setErrorDetails("An unexpected error occurred. Please try again.");
      }
    }
  }

  async function handleConfirmDelete() {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
      handleCloseDelete();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorDetails(error.detail);

        if (error.modelErrors) {
          setModelErrors(error.modelErrors);
        }
      } else {
        ClientLogger.LogError({
          message: "Unexpected error while submitting the request",
          details: error instanceof Error ? error.message : String(error),
          context: "From handleConfirmDelete()",
          path: "/budgets",
          statusCode: AppStatusCode.ClientError,
        } as ClientLogEntry);
        setErrorDetails("An unexpected error occurred. Please try again.");
      }
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Budgets</h1>
        <button
          onClick={handleOpenCreate}
          className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Add Budget
        </button>
      </div>

      <BudgetList
        budgets={budgets}
        isLoading={isLoading}
        error={error}
        onEdit={handleOpenEdit}
        onDelete={setDeleteTarget}
      />

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl max-h-[90dvh] overflow-y-auto">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900">
                {editingBudget ? "Edit Budget" : "New Budget"}
              </h2>
              <button
                onClick={handleCloseModal}
                className="rounded-md p-1 text-gray-400 hover:text-gray-600"
                aria-label="Close"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            {errorDetails && (
              <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {errorDetails}
              </div>
            )}
            <BudgetForm
              defaultValues={
                editingBudget
                  ? {
                      categoryId: editingBudget.categoryId,
                      name: editingBudget.name,
                      limitAmount: editingBudget.limitAmount,
                      period: editingBudget.period,
                    }
                  : undefined
              }
              onSubmit={handleSubmit}
              isSubmitting={isSubmitting}
              submitLabel={editingBudget ? "Update Budget" : "Create Budget"}
              modelErrors={modelErrors}
            />
          </div>
        </div>
      )}

      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
            <h2 className="text-lg font-semibold text-gray-900">Delete Budget</h2>
            <p className="mt-2 text-sm text-gray-600">
              Are you sure you want to delete "{deleteTarget.name}"?
            </p>
            {errorDetails && (
              <div className="mt-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {errorDetails}
              </div>
            )}
            <div className="mt-6 flex gap-3">
              <button
                onClick={handleCloseDelete}
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
