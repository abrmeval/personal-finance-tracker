import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
} from "@/types/auth";

import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";

export const authApi = {
  login: (data: LoginRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/login", data, true),
  register: (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/register", data, true),
  refresh: (data: RefreshTokenRequest): Promise<ApiResponse<AuthResponse>> => apiClient.post<AuthResponse>("/auth/refresh", data, true),
  revoke: (data: RefreshTokenRequest): Promise<ApiResponse<void>> => apiClient.post<void>("/auth/revoke", data),
};
