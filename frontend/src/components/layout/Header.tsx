import { LogOut, User } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";

export function Header() {
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
  }

  return (
    <header className="h-14 bg-white border-b border-gray-200 flex items-center justify-between px-6">
      <h2 className="text-sm font-medium text-gray-500">
        Personal Finance Tracker
      </h2>
      {user && (
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-sm text-gray-700">
            <User className="w-4 h-4 text-gray-400" aria-hidden="true" />
            <span>
              {user.firstName} {user.lastName}
            </span>
          </div>
          <button
            onClick={handleLogout}
            aria-label="Sign out"
            className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition-colors"
          >
            <LogOut className="w-4 h-4" aria-hidden="true" />
            <span>Sign out</span>
          </button>
        </div>
      )}
    </header>
  );
}
