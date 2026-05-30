import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "@/hooks/useAuth";
import { loginSchema } from "@/features/auth/schemas";
import type { LoginFormData } from "@/features/auth/schemas";
import { ApiError, AppStatusCode } from "@/types/http";
import { ClientLogger, type ClientLogEntry } from "@/utils/clientLogger";
import { setDocumentTitle } from "@/utils/documentTitle";

export function LoginPage() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);
  const [modelErrors, setModelErrors] = useState<Record<string, string[]> | null>(null);

  setDocumentTitle("Login page");

  useEffect(() => {
    if (isAuthenticated) navigate("/", { replace: true });
  }, [isAuthenticated, navigate]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  async function onSubmit(data: LoginFormData) {
    setServerError(null);
    setModelErrors(null);

    try {
      await login(data);
      navigate("/", { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setServerError(error.title);

        if (error.modelErrors) {
          setModelErrors(error.modelErrors);
        }
      } else {
        ClientLogger.LogError({
          message: "Unexpected error during login",
          error: error instanceof Error ? error : String(error),
          context: "[onSubmit]",
          path: "/auth/login",
          statusCode: AppStatusCode.ClientError,
        } as ClientLogEntry);
        setServerError("An unexpected error occurred. Please try again.");
      }
    }
  }

  return (
    <div className="min-h-[100dvh] flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Welcome back</h1>
        <p className="text-sm text-gray-500 mb-6">Sign in to your account</p>
        {serverError && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {serverError}
          </div>
        )}
        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-4"
          noValidate
        >
          <div>
            <label
              htmlFor="email"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              {...register("email")}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              placeholder="you@example.com"
            />
            {errors.email && (
              <p className="mt-1 text-xs text-red-600">
                {errors.email.message}
              </p>
            )}

            {modelErrors?.email &&
              modelErrors.email.map((msg, idx) => (
                <p key={idx} className="text-xs text-red-600">
                  {msg}
                </p>
              ))}
          </div>
          <div>
            <label
              htmlFor="password"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Password
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              {...register("password")}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              placeholder="••••••••"
            />
            {errors.password && (
              <p className="mt-1 text-xs text-red-600">
                {errors.password.message}
              </p>
            )}
            {modelErrors?.password &&
              modelErrors.password.map((msg, idx) => (
                <p key={idx} className="text-xs text-red-600">
                  {msg}
                </p>
              ))}
          </div>
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isSubmitting ? "Signing in…" : "Sign in"}
          </button>
        </form>
        <p className="mt-6 text-center text-sm text-gray-500">
          Don't have an account?{" "}
          <Link
            to="/register"
            className="font-medium text-indigo-600 hover:text-indigo-500"
          >
            Create one
          </Link>
        </p>
      </div>
    </div>
  );
}
