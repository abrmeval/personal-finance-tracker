import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/http";
import type {
  Transaction,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  TransactionFilters,
} from "@/types/finance";

function buildQueryString(filters: TransactionFilters): string {
  const params = new URLSearchParams();
  params.set("page", String(filters.page));
  params.set("pageSize", String(filters.pageSize));

  if (filters.startDate) params.set("startDate", filters.startDate);
  if (filters.endDate) params.set("endDate", filters.endDate);
  if (filters.categoryId) params.set("categoryId", filters.categoryId);
  if (filters.type) params.set("type", filters.type);

  return params.toString();
}

export const transactionsApi = {
  getAll: (
    filters: TransactionFilters,
  ): Promise<ApiResponse<PagedResult<Transaction>>> =>
    apiClient.get<PagedResult<Transaction>>(
      `/transactions?${buildQueryString(filters)}`,
    ),

  getById: (id: string): Promise<ApiResponse<Transaction>> =>
    apiClient.get<Transaction>(`/transactions/${id}`),

  create: (data: CreateTransactionRequest): Promise<ApiResponse<Transaction>> =>
    apiClient.post<Transaction>("/transactions", data),

  update: (
    id: string,
    data: UpdateTransactionRequest,
  ): Promise<ApiResponse<Transaction>> =>
    apiClient.put<Transaction>(`/transactions/${id}`, data),

  delete: (id: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/transactions/${id}`),
};
