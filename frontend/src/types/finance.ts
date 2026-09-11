export type TransactionType = 'Income' | 'Expense';

export interface Category {
  id: string;
  name: string;
  icon: string | null;
  color: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface Transaction {
  id: string;
  description: string;
  amount: number;
  type: TransactionType;
  date: string;
  categoryId: string | null;
  categoryName: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  icon: string | null;
  color: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  icon: string | null;
  color: string | null;
}

export interface CreateTransactionRequest {
  description: string;
  amount: number;
  type: TransactionType;
  date: string;
  categoryId: string | null;
  notes: string | null;
}

export interface UpdateTransactionRequest {
  description: string;
  amount: number;
  type: TransactionType;
  date: string;
  categoryId: string | null;
  notes: string | null;
}

export interface TransactionFilters {
  page: number;
  pageSize: number;
  startDate?: string;
  endDate?: string;
  categoryId?: string;
  type?: TransactionType;
}

export type BudgetPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export interface Budget {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  limitAmount: number;
  period: BudgetPeriod;
  createdAt: string;
  updatedAt: string | null;
}

export interface BudgetWithSpending extends Budget {
  spentAmount: number;
  remainingAmount: number;
  percentageUsed: number;
  isOverBudget: boolean;
}

export interface CreateBudgetRequest {
  categoryId: string;
  name: string;
  limitAmount: number;
  period: BudgetPeriod;
}

export interface UpdateBudgetRequest {
  categoryId: string;
  name: string;
  limitAmount: number;
  period: BudgetPeriod;
}