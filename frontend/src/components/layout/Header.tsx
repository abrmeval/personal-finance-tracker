import { LogOut, User } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";

export function Header() {
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
  }

  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-3 border-b border-gray-200 bg-white px-4 md:px-6">
      <h2 className="min-w-0 truncate text-sm font-medium text-gray-500">
        Personal Finance Tracker
      </h2>
      {user && (
        <div className="flex min-w-0 shrink-0 items-center gap-2 sm:gap-3">
          <div className="flex min-w-0 items-center gap-2 text-sm text-gray-700">
            <User className="h-4 w-4 shrink-0 text-gray-400" aria-hidden="true" />
            <span className="max-w-28 truncate sm:max-w-none">
              {user.firstName} {user.lastName}
            </span>
          </div>
          <button
            onClick={handleLogout}
            aria-label="Sign out"
            className="inline-flex min-h-11 min-w-11 items-center justify-center gap-1.5 rounded-lg px-2 text-sm text-gray-600 transition-colors hover:bg-gray-100 hover:text-gray-900 sm:px-3"
          >
            <LogOut className="h-4 w-4" aria-hidden="true" />
            <span className="hidden sm:inline">Sign out</span>
          </button>
        </div>
      )}
    </header>
  );
}
