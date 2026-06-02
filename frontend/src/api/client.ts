import type { AuthResponse } from "@/types/auth";
import { AppStatusCode, ApiError, type ApiResponse } from "@/types/http";
import { ClientLogger } from "@/utils/clientLogger";

const BASE_URL = import.meta.env.VITE_API_URL ?? "/api";

let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const storedRefreshToken = localStorage.getItem("refreshToken");

  if (!storedRefreshToken) {
    throw new ApiError(
      "Please log in again.",
      "No refresh token available.",
      "[refreshAccessToken]",
      "/auth/refresh",
      AppStatusCode.ClientError,
    );
  }

  const response = await fetch(`${BASE_URL}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: storedRefreshToken }),
  });

  if (!response.ok) {
    throw new ApiError(
      "Unauthorized. Please log in again.",
      response.statusText,
      "[refreshAccessToken]",
      "/auth/refresh",
      response.status,
    );
  }
  const responseBody = (await response.json()) as ApiResponse<AuthResponse>;

  if (!responseBody.data) return "";

  localStorage.setItem("accessToken", responseBody.data.accessToken);
  localStorage.setItem("refreshToken", responseBody.data.refreshToken);
  return responseBody.data.accessToken;
}

async function getValidAccessToken(): Promise<string | null> {
  const token = localStorage.getItem("accessToken");
  return token;
}

async function parseResponseAsync<T>(
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

  const responseBody = (await response.json()) as ApiResponse<T>;

  if (!response.ok) {
    throw new ApiError(
      responseBody.error?.title || "Error",
      responseBody.error?.detail || response.statusText,
      "[parseResponse]",
      path,
      response?.status,
    );
  }

  return responseBody;
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  anonymous : boolean = false,
  retry = true,
): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  let response;

  try {
    // If the request is marked as anonymous, skip attaching tokens and directly make the request
    if (anonymous) {
      response = await fetch(`${BASE_URL}${path}`, { ...options, headers });
      return await parseResponseAsync<T>(response, path);
    }

    const token = await getValidAccessToken();
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }

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
          "That was an unauthorized request. Please log in again.",
          retried.statusText,
          "[request]",
          path,
          retried.status,
        );
      }
      return await parseResponseAsync<T>(retried);
    }
    return await parseResponseAsync<T>(response);
  } catch (error) {
    if (error instanceof TypeError) {
      const apiError = new ApiError(
        "There was a network error.",
        error.message,
        "[request]",
        path,
        AppStatusCode.NetworkError,
      );

      ClientLogger.LogWarning({
        message: apiError.title,
        statusCode: AppStatusCode.NetworkError,
        details: error.message,
        path: apiError.instance,
        context: apiError.context,
      });
      throw apiError;
    }

    if (error instanceof ApiError) {
      if (error.status === AppStatusCode.Unauthorized) {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("user");

        if(!anonymous)
          window.location.href = "/login";
      }

      ClientLogger.LogError({
        message: error.title,
        statusCode: error.status,
        details: error.detail,
        path: error.instance,
        context: error.context,
      });
      throw error;
    }

    const apiError = new ApiError(
      "An unexpected error occurred.",
      error instanceof Error ? error.message : String(error),
      "[request]",
      path,
      response?.status,
    );

    ClientLogger.LogError({
      message: apiError.title,
      statusCode: response?.status,
      details: apiError.message,
      path: apiError.instance,
      context: apiError.context,
    });
    throw apiError;
  }
}

export const apiClient = {
  get: <T>(path: string, anonymous?: boolean) =>
    request<T>(path, { method: "GET" }, anonymous),
  post: <T>(path: string, body?: unknown, anonymous?: boolean) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, anonymous),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }),
  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PATCH", body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
