import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";
import type {
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/types/finance";

export const categoriesApi = {
  getAll: (): Promise<ApiResponse<Category[]>> =>
    apiClient.get<Category[]>("/categories"),

  getById: (id: string): Promise<ApiResponse<Category>> =>
    apiClient.get<Category>(`/categories/${id}`),

  create: (data: CreateCategoryRequest): Promise<ApiResponse<Category>> =>
    apiClient.post<Category>("/categories", data),

  update: (
    id: string,
    data: UpdateCategoryRequest,
  ): Promise<ApiResponse<Category>> =>
    apiClient.put<Category>(`/categories/${id}`, data),

  delete: (id: string): Promise<ApiResponse<void>> =>
    apiClient.delete<void>(`/categories/${id}`),
};
