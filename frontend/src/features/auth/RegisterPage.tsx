import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "@/hooks/useAuth";
import { registerSchema } from "@/features/auth/schemas";
import type { RegisterFormData } from "@/features/auth/schemas";
import { ApiError, AppStatusCode } from "@/types/http";
import { ClientLogger, type ClientLogEntry } from "@/utils/clientLogger";
import { setDocumentTitle } from "@/utils/documentTitle";

export function RegisterPage() {
  const { register: registerUser, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [errorTitle, setErrorTitle] = useState<string | null>(null);
  const [modelErrors, setModelErrors] = useState<Record<
    string,
    string[]
  > | null>(null);
  const [errorDetails, setErrorDetails] = useState<string | null>(null);

  setDocumentTitle("Register page");

  useEffect(() => {
    if (isAuthenticated) navigate("/", { replace: true });
  }, [isAuthenticated, navigate]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  async function onSubmit(data: RegisterFormData) {
    setErrorTitle(null);
    setModelErrors(null);
    setErrorDetails(null);

    try {
      await registerUser({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
      });
      navigate("/", { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorTitle(error.title);
        setErrorDetails(error.detail);

        if (error.modelErrors) {
          setModelErrors(error.modelErrors);
        }
      } else {
        ClientLogger.LogError({
          message: "Unexpected error during registration",
          details: error instanceof Error ? error.message : String(error),
          context: "[onSubmit]",
          path: "/auth/register",
          statusCode: AppStatusCode.ClientError,
        } as ClientLogEntry);
        setErrorTitle("An unexpected error occurred. Please try again.");
      }
    }
  }
  return (
    <div className="min-h-[100dvh] flex items-center justify-center bg-gray-50 px-4 py-8">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          Create an account
        </h1>
        <p className="text-sm text-gray-500 mb-6">
          Start tracking your finances today
        </p>
        {errorTitle && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {errorTitle}
            {errorDetails && (
              <p className="mt-1 text-xs text-red-600">{errorDetails}</p>
            )}
          </div>
        )}
        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-4"
          noValidate
        >
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="firstName"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                First name
              </label>
              <input
                id="firstName"
                type="text"
                autoComplete="given-name"
                {...register("firstName")}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                placeholder="Jane"
              />
              {errors.firstName && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.firstName.message}
                </p>
              )}

              {modelErrors?.firstName &&
                modelErrors.firstName.map((msg, idx) => (
                  <p key={idx} className="text-xs text-red-600">
                    {msg}
                  </p>
                ))}
            </div>
            <div>
              <label
                htmlFor="lastName"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Last name
              </label>
              <input
                id="lastName"
                type="text"
                autoComplete="family-name"
                {...register("lastName")}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                placeholder="Doe"
              />
              {errors.lastName && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.lastName.message}
                </p>
              )}

              {modelErrors?.lastName &&
                modelErrors.lastName.map((msg, idx) => (
                  <p key={idx} className="text-xs text-red-600">
                    {msg}
                  </p>
                ))}
            </div>
          </div>
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
              autoComplete="new-password"
              {...register("password")}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              placeholder="Min. 8 characters"
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
          <div>
            <label
              htmlFor="confirmPassword"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Confirm password
            </label>
            <input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              {...register("confirmPassword")}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              placeholder="••••••••"
            />
            {errors.confirmPassword && (
              <p className="mt-1 text-xs text-red-600">
                {errors.confirmPassword.message}
              </p>
            )}

            {modelErrors?.confirmPassword &&
              modelErrors.confirmPassword.map((msg, idx) => (
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
            {isSubmitting ? "Creating account…" : "Create account"}
          </button>
        </form>
        <p className="mt-6 text-center text-sm text-gray-500">
          Already have an account?{" "}
          <Link
            to="/login"
            className="font-medium text-indigo-600 hover:text-indigo-500"
          >
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
