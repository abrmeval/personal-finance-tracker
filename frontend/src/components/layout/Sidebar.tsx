import {
  ChartNoAxesCombined,
  LayoutDashboard,
  List,
  Tags,
  WalletCards,
} from "lucide-react";
import { NavLink } from "react-router-dom";

const NAV_ITEMS = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/transactions", label: "Transactions", icon: List },
  { to: "/categories", label: "Categories", icon: Tags },
  { to: "/budgets", label: "Budgets", icon: WalletCards },
  { to: "/reports", label: "Reports", icon: ChartNoAxesCombined },
];

export function Sidebar() {
  return (
    <aside className="flex h-14 w-full shrink-0 flex-row items-center bg-gray-900 px-3 text-white md:h-[100dvh] md:w-64 md:flex-col md:items-stretch md:p-4">
      <h1 className="shrink-0 text-base font-bold md:mb-8 md:text-xl">
        <span className="md:hidden">FT</span>
        <span className="hidden md:inline">Finance Tracker</span>
      </h1>
      <nav className="ml-3 flex min-w-0 flex-1 items-center justify-end gap-1 overflow-x-auto md:ml-0 md:flex-col md:items-stretch md:justify-start">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              aria-label={item.label}
              className={({ isActive }) =>
                `inline-flex min-h-11 shrink-0 items-center justify-center gap-2 rounded-md px-2 text-sm font-medium transition-colors md:justify-start md:px-3 ${
                  isActive
                    ? "bg-indigo-600 text-white"
                    : "text-gray-300 hover:bg-gray-700"
                }`
              }
            >
              <Icon className="h-5 w-5" aria-hidden="true" />
              <span className="hidden md:inline">{item.label}</span>
            </NavLink>
          );
        })}
      </nav>
    </aside>
  );
}
