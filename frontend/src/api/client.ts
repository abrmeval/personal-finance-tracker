import type { AuthResponse } from "@/types/auth";
import { AppStatusCode, ApiError, type ApiResponse } from "@/types/http";
import { ClientLogger, type ClientLogEntry } from "@/utils/clientLogger";

const BASE_URL = import.meta.env.VITE_API_URL ?? "/api";

let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const storedRefreshToken = localStorage.getItem("refreshToken");

  if (!storedRefreshToken) {
    throw new ApiError("No refresh token available.", "[refreshAccessToken]");
  }

  const response = await fetch(`${BASE_URL}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: storedRefreshToken }),
  });

  if (!response.ok) {
    throw new ApiError(
      "Token refresh failed: " + response.statusText,
      "[refreshAccessToken]",
      "/auth/refresh",
      response.status,
    );
  }
  const data = (await response.json()) as AuthResponse;
  localStorage.setItem("accessToken", data.accessToken);
  localStorage.setItem("refreshToken", data.refreshToken);
  return data.accessToken;
}

async function getValidAccessToken(): Promise<string | null> {
  const token = localStorage.getItem("accessToken");
  return token;
}

async function parseResponse<T>(
  response: Response,
  path?: string,
): Promise<ApiResponse<T>> {
  // Handle 204 No Content separately since it has no body
  if (response.status === AppStatusCode.NoContent) {
    return {
      isOk: response.ok,
      data: null,
      statusCode: response.status,
    } as ApiResponse<T>;
  }

  let responseBody;
  try {
    responseBody = (await response.json()) as ApiResponse<T>;
  } catch (error) {
    throw new ApiError(
      "Parsing error:" +
        (error instanceof Error ? error.message : String(error)),
      "[parseResponse]",
      path,
      AppStatusCode.ParseError,
    );
  }

  if (!response.ok) {
    //loging the error response for debugging
    const log = {
      message: "API response error.",
      statusCode: response.status,
      originalMessage: responseBody?.error?.message,
      path: responseBody.error?.path || path,
    } as ClientLogEntry;

    if (response.status >= 500) ClientLogger.LogError(log);
    else if (response.status >= 400) ClientLogger.LogWarning(log);
    else ClientLogger.LogInfo(log);
  }
  return responseBody;
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  retry = true,
): Promise<ApiResponse<T>> {
  const token = await getValidAccessToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  let response;

  try {
    //First fetch attempt
    response = await fetch(`${BASE_URL}${path}`, { ...options, headers });

    if (response.status === AppStatusCode.Unauthorized && retry) {
      // Deduplicate concurrent refresh calls
      if (!isRefreshing) {
        isRefreshing = true;
        refreshPromise = refreshAccessToken().finally(() => {
          isRefreshing = false;
          refreshPromise = null;
        });
      }

      const newToken = await refreshPromise!;
      headers["Authorization"] = `Bearer ${newToken}`;

      // Retry the original request with the new token
      const retried = await fetch(`${BASE_URL}${path}`, {
        ...options,
        headers,
      });

      // If the retried request also fails with 401, throw an error to trigger logout
      if (retried.status === AppStatusCode.Unauthorized) {
        throw new ApiError(
          "Retried request failed: " + retried.statusText,
          "[request]",
          path,
          retried.status,
        );
      }
      return parseResponse<T>(retried);
    }
    return parseResponse<T>(response);
  } catch (error) {
    if (error instanceof TypeError) {
      const apiError = new ApiError(
        "Network error or CORS issue.",
        "[request]",
        path,
        AppStatusCode.NetworkError,
      );

      ClientLogger.LogError({
        message: apiError.message,
        statusCode: AppStatusCode.NetworkError,
        originalMessage: error.message,
        path: apiError.path,
      });
      throw apiError;
    }

    if (error instanceof ApiError) {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("user");
      window.location.href = "/login";

      ClientLogger.LogError({
        message: error.message,
        statusCode: AppStatusCode.NetworkError,
        originalMessage: error.message,
        path: error.path,
      });
      throw error;
    }

    const apiError = new ApiError(
      "An unexpected error occurred",
      "[request]",
      path,
      response?.status,
    );

    ClientLogger.LogError({
      message: apiError.message,
      statusCode: response?.status,
      originalMessage: error instanceof Error ? error.message : String(error),
      path: apiError.path,
    });
    throw apiError;
  }
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }),
  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PATCH", body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
