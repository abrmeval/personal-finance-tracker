import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
} from "@/types/auth";

import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";

export const authApi = {
  login: (data: LoginRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/login", data),
  register: (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/register", data),
  refresh: (data: RefreshTokenRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/refresh", data),
  revoke: (data: RefreshTokenRequest): Promise<ApiResponse<void>> => apiClient.post<void>("/auth/revoke", data),
};
