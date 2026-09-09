import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { budgetsApi } from '@/api/budgets';
import type { CreateBudgetRequest, UpdateBudgetRequest } from '@/types/finance';

export const budgetKeys = {
  all: ['budgets'] as const,
  lists: () => [...budgetKeys.all, 'list'] as const,
  details: () => [...budgetKeys.all, 'detail'] as const,
  detail: (id: string) => [...budgetKeys.all, 'detail', id] as const,
};

export function useBudgets() {
  return useQuery({
    queryKey: budgetKeys.lists(),
    queryFn: () => budgetsApi.getAll(),
    staleTime: 1000 * 60 * 2, // 2 minutes — spending data changes with transactions
  });
}

export function useCreateBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBudgetRequest) => budgetsApi.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
    },
  });
}

export function useUpdateBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBudgetRequest }) =>
      budgetsApi.update(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
    },
  });
}

export function useDeleteBudget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => budgetsApi.delete(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
    },
  });
}
