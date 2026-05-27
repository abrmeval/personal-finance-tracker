import { useState } from 'react';
import type { ReactNode } from 'react';
import type { UserResponse, LoginRequest, RegisterRequest } from '@/types/auth';
import { authApi } from '@/api/auth';
import { AuthContext } from './authContext';

interface AuthProviderProps {
  children: ReactNode;
}

function getStoredUser(): UserResponse | null {
  const stored = localStorage.getItem('user');
  if (!stored) return null;
  try {
    return JSON.parse(stored) as UserResponse;
  } catch {
    localStorage.removeItem('user');
    return null;
  }
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<UserResponse | null>(getStoredUser);
  const isLoading = false;

  async function login(data: LoginRequest) {
    const response = await authApi.login(data);

    if (!response.isOk || !response.data)
      return;

    const { accessToken, refreshToken, user } = response.data;

    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('user', JSON.stringify(user));
    setUser(user);
  }

  async function register(data: RegisterRequest) {
    const response = await authApi.register(data);
    if (!response.isOk || !response.data)
      return;

    const { accessToken, refreshToken, user } = response.data;

    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('user', JSON.stringify(user));
    setUser(user);
  }

  async function logout() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await authApi.revoke({ refreshToken });
      } catch {
        // best-effort revocation — clear locally regardless
      }
    }
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: user !== null,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
