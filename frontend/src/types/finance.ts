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