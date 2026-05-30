// src/types/auth.ts
// Authentication-related TypeScript types for the frontend application
export interface UserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserResponse;
}
export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}
export interface RefreshTokenRequest {
  refreshToken: string;
}