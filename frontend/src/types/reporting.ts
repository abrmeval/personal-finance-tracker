export interface DashboardSummary {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
}

export interface MonthlyTotals {
  year: number;
  month: number;
  income: number;
  expenses: number;
}

export interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  total: number;
}
