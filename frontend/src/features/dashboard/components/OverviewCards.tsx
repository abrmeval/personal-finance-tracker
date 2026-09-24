import { TrendingDown, TrendingUp, Wallet } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { DashboardSummary } from "@/types/reporting";
import { formatCurrency } from "@/utils/formatters";

interface OverviewCardsProps {
  summary: DashboardSummary | null;
  isLoading: boolean;
}

interface SummaryCard {
  title: string;
  value: number;
  icon: LucideIcon;
  iconBg: string;
  valueText: string;
}

export function OverviewCards({ summary, isLoading }: OverviewCardsProps) {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3" aria-busy="true">
        {[1, 2, 3].map((item) => (
          <div key={item} className="h-28 animate-pulse rounded-lg bg-gray-100" />
        ))}
      </div>
    );
  }

  const cards: SummaryCard[] = [
    {
      title: "Total Balance",
      value: summary?.totalBalance ?? 0,
      icon: Wallet,
      iconBg: "bg-indigo-500",
      valueText: "text-indigo-600",
    },
    {
      title: "Monthly Income",
      value: summary?.monthlyIncome ?? 0,
      icon: TrendingUp,
      iconBg: "bg-green-500",
      valueText: "text-green-600",
    },
    {
      title: "Monthly Expenses",
      value: summary?.monthlyExpenses ?? 0,
      icon: TrendingDown,
      iconBg: "bg-red-500",
      valueText: "text-red-600",
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
      {cards.map((card) => (
        <div
          key={card.title}
          className="rounded-lg border border-gray-200 bg-white px-4 py-5"
        >
          <div className="flex items-center justify-between gap-3">
            <div className="min-w-0">
              <p className="text-sm font-medium text-gray-500">{card.title}</p>
              <p className={`truncate text-2xl font-bold ${card.valueText}`}>
                {formatCurrency(card.value)}
              </p>
            </div>
            <div className={`shrink-0 rounded-full p-3 ${card.iconBg}`}>
              <card.icon className="h-6 w-6 text-white" aria-hidden="true" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
