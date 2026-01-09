# Personal Finance Tracker - Frontend Documentation

> **React 18** | Vite • TypeScript • TanStack Query • React Hook Form • Chart.js  
> Tailwind CSS • Azure Static Web Apps

---

## 1. Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Framework** | React 18 | UI library |
| **Build Tool** | Vite | Fast development & bundling |
| **Language** | TypeScript | Type safety |
| **Styling** | Tailwind CSS | Utility-first CSS |
| **Data Fetching** | TanStack Query (React Query) | Server state management |
| **Forms** | React Hook Form + Zod | Form handling & validation |
| **Charts** | Chart.js + react-chartjs-2 | Data visualization |
| **Routing** | React Router v6 | Client-side routing |
| **HTTP Client** | Axios | API requests |
| **Icons** | Lucide React | Icon library |

---

## 2. Project Structure

```
frontend/
│
├── 📁 src/
│   ├── 📁 api/                      # API client and endpoints
│   │   ├── 📄 client.ts             # Axios instance configuration
│   │   ├── 📄 transactions.ts       # Transaction API calls
│   │   ├── 📄 categories.ts         # Category API calls
│   │   ├── 📄 budgets.ts            # Budget API calls
│   │   ├── 📄 reports.ts            # Reporting API calls
│   │   └── 📄 auth.ts               # Authentication API calls
│   │
│   ├── 📁 components/               # Reusable UI components
│   │   ├── 📁 ui/                   # Base UI components
│   │   │   ├── 📄 Button.tsx
│   │   │   ├── 📄 Input.tsx
│   │   │   ├── 📄 Card.tsx
│   │   │   ├── 📄 Modal.tsx
│   │   │   ├── 📄 Table.tsx
│   │   │   └── 📄 Loading.tsx
│   │   ├── 📁 forms/                # Form components
│   │   │   ├── 📄 TransactionForm.tsx
│   │   │   ├── 📄 CategoryForm.tsx
│   │   │   └── 📄 BudgetForm.tsx
│   │   ├── 📁 charts/               # Chart components
│   │   │   ├── 📄 SpendingPieChart.tsx
│   │   │   ├── 📄 IncomeExpenseChart.tsx
│   │   │   └── 📄 TrendLineChart.tsx
│   │   └── 📁 layout/               # Layout components
│   │       ├── 📄 Sidebar.tsx
│   │       ├── 📄 Header.tsx
│   │       └── 📄 MainLayout.tsx
│   │
│   ├── 📁 features/                 # Feature-specific components
│   │   ├── 📁 dashboard/
│   │   │   ├── 📄 Dashboard.tsx
│   │   │   ├── 📄 OverviewCards.tsx
│   │   │   └── 📄 RecentTransactions.tsx
│   │   ├── 📁 transactions/
│   │   │   ├── 📄 TransactionList.tsx
│   │   │   ├── 📄 TransactionFilters.tsx
│   │   │   └── 📄 TransactionModal.tsx
│   │   ├── 📁 categories/
│   │   │   └── 📄 CategoryManager.tsx
│   │   ├── 📁 budgets/
│   │   │   ├── 📄 BudgetList.tsx
│   │   │   └── 📄 BudgetProgress.tsx
│   │   └── 📁 reports/
│   │       ├── 📄 MonthlyReport.tsx
│   │       └── 📄 CategoryBreakdown.tsx
│   │
│   ├── 📁 hooks/                    # Custom React hooks
│   │   ├── 📄 useAuth.ts
│   │   ├── 📄 useTransactions.ts
│   │   ├── 📄 useBudgets.ts
│   │   └── 📄 useDebounce.ts
│   │
│   ├── 📁 context/                  # React context providers
│   │   └── 📄 AuthContext.tsx
│   │
│   ├── 📁 types/                    # TypeScript type definitions
│   │   ├── 📄 transaction.ts
│   │   ├── 📄 category.ts
│   │   ├── 📄 budget.ts
│   │   ├── 📄 user.ts
│   │   └── 📄 api.ts
│   │
│   ├── 📁 utils/                    # Utility functions
│   │   ├── 📄 formatters.ts
│   │   ├── 📄 dateUtils.ts
│   │   └── 📄 validators.ts
│   │
│   ├── 📁 styles/                   # Global styles
│   │   └── 📄 globals.css
│   │
│   ├── 📄 App.tsx                   # Root component
│   ├── 📄 main.tsx                  # Entry point
│   └── 📄 routes.tsx                # Route definitions
│
├── 📁 public/                       # Static assets
├── 📄 index.html
├── 📄 package.json
├── 📄 tsconfig.json
├── 📄 vite.config.ts
├── 📄 tailwind.config.js
└── 📄 .env.example
```

---

## 3. Initial Setup

### Create Vite Project

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
```

### Install Dependencies

```bash
# Core dependencies
npm install react-router-dom @tanstack/react-query axios

# Forms and validation
npm install react-hook-form @hookform/resolvers zod

# Charts
npm install chart.js react-chartjs-2

# UI utilities
npm install clsx tailwind-merge lucide-react

# Date handling
npm install date-fns

# Development dependencies
npm install -D tailwindcss postcss autoprefixer
npm install -D @types/node
```

### Tailwind Configuration

```bash
npx tailwindcss init -p
```

```javascript
// tailwind.config.js
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        success: '#059669',
        warning: '#d97706',
        danger: '#dc2626',
      },
    },
  },
  plugins: [],
}
```

### Vite Configuration

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
```

---

## 4. API Client Setup

### Axios Instance

```typescript
// src/api/client.ts
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Handle 401 - token expired
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
          refreshToken,
        });

        const { accessToken } = response.data;
        localStorage.setItem('accessToken', accessToken);

        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
```

### Transaction API

```typescript
// src/api/transactions.ts
import { apiClient } from './client';
import type { 
  Transaction, 
  CreateTransactionRequest, 
  UpdateTransactionRequest,
  TransactionQueryParams,
  PagedResult 
} from '@/types';

export const transactionsApi = {
  getAll: async (params?: TransactionQueryParams): Promise<PagedResult<Transaction>> => {
    const response = await apiClient.get('/transactions', { params });
    return response.data;
  },

  getById: async (id: string): Promise<Transaction> => {
    const response = await apiClient.get(`/transactions/${id}`);
    return response.data;
  },

  create: async (data: CreateTransactionRequest): Promise<Transaction> => {
    const response = await apiClient.post('/transactions', data);
    return response.data;
  },

  update: async (id: string, data: UpdateTransactionRequest): Promise<void> => {
    await apiClient.put(`/transactions/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/transactions/${id}`);
  },
};
```

### Reports API

```typescript
// src/api/reports.ts
import { apiClient } from './client';
import type { 
  DashboardSummary, 
  MonthlySummary, 
  CategoryBreakdown 
} from '@/types';

export const reportsApi = {
  getDashboardSummary: async (): Promise<DashboardSummary> => {
    const response = await apiClient.get('/reports/dashboard');
    return response.data;
  },

  getMonthlySummary: async (year: number, month: number): Promise<MonthlySummary> => {
    const response = await apiClient.get(`/reports/monthly/${year}/${month}`);
    return response.data;
  },

  getCategoryBreakdown: async (
    startDate: string,
    endDate: string
  ): Promise<CategoryBreakdown[]> => {
    const response = await apiClient.get('/reports/categories', {
      params: { startDate, endDate },
    });
    return response.data;
  },

  getIncomeVsExpenses: async (
    months: number = 6
  ): Promise<{ month: string; income: number; expenses: number }[]> => {
    const response = await apiClient.get('/reports/income-vs-expenses', {
      params: { months },
    });
    return response.data;
  },
};
```

---

## 5. TanStack Query Setup

### Query Client Provider

```typescript
// src/main.tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import './styles/globals.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  </React.StrictMode>
);
```

### Custom Hooks with React Query

```typescript
// src/hooks/useTransactions.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { transactionsApi } from '@/api/transactions';
import type { 
  CreateTransactionRequest, 
  UpdateTransactionRequest,
  TransactionQueryParams 
} from '@/types';

export const transactionKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionKeys.all, 'list'] as const,
  list: (params: TransactionQueryParams) => [...transactionKeys.lists(), params] as const,
  details: () => [...transactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...transactionKeys.details(), id] as const,
};

export function useTransactions(params?: TransactionQueryParams) {
  return useQuery({
    queryKey: transactionKeys.list(params ?? {}),
    queryFn: () => transactionsApi.getAll(params),
  });
}

export function useTransaction(id: string) {
  return useQuery({
    queryKey: transactionKeys.detail(id),
    queryFn: () => transactionsApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTransactionRequest) => transactionsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}

export function useUpdateTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTransactionRequest }) =>
      transactionsApi.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: transactionKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}

export function useDeleteTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => transactionsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}
```

### Dashboard Hook

```typescript
// src/hooks/useDashboard.ts
import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '@/api/reports';

export function useDashboardSummary() {
  return useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => reportsApi.getDashboardSummary(),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

export function useIncomeVsExpenses(months: number = 6) {
  return useQuery({
    queryKey: ['dashboard', 'income-vs-expenses', months],
    queryFn: () => reportsApi.getIncomeVsExpenses(months),
  });
}

export function useCategoryBreakdown(startDate: string, endDate: string) {
  return useQuery({
    queryKey: ['dashboard', 'categories', startDate, endDate],
    queryFn: () => reportsApi.getCategoryBreakdown(startDate, endDate),
    enabled: !!startDate && !!endDate,
  });
}
```

---

## 6. React Hook Form with Zod

### Form Schemas

```typescript
// src/utils/validators.ts
import { z } from 'zod';

export const transactionSchema = z.object({
  description: z
    .string()
    .min(1, 'Description is required')
    .max(500, 'Description cannot exceed 500 characters'),
  amount: z
    .number({ invalid_type_error: 'Amount must be a number' })
    .positive('Amount must be greater than zero')
    .max(1_000_000_000, 'Amount exceeds maximum allowed'),
  type: z.enum(['Income', 'Expense'], {
    errorMap: () => ({ message: 'Please select a transaction type' }),
  }),
  date: z.string().min(1, 'Date is required'),
  categoryId: z.string().optional(),
});

export const budgetSchema = z.object({
  name: z
    .string()
    .min(1, 'Budget name is required')
    .max(100, 'Name cannot exceed 100 characters'),
  amount: z.number().positive('Amount must be greater than zero'),
  period: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly']),
  startDate: z.string().min(1, 'Start date is required'),
  endDate: z.string().optional(),
  categoryId: z.string().min(1, 'Category is required'),
});

export type TransactionFormData = z.infer<typeof transactionSchema>;
export type BudgetFormData = z.infer<typeof budgetSchema>;
```

### Transaction Form Component

```tsx
// src/components/forms/TransactionForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { transactionSchema, type TransactionFormData } from '@/utils/validators';
import { useCategories } from '@/hooks/useCategories';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';

interface TransactionFormProps {
  defaultValues?: Partial<TransactionFormData>;
  onSubmit: (data: TransactionFormData) => void;
  isLoading?: boolean;
}

export function TransactionForm({ 
  defaultValues, 
  onSubmit, 
  isLoading 
}: TransactionFormProps) {
  const { data: categories } = useCategories();

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: {
      type: 'Expense',
      date: new Date().toISOString().split('T')[0],
      ...defaultValues,
    },
  });

  const transactionType = watch('type');

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* Transaction Type */}
      <div className="flex gap-4">
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="radio"
            value="Expense"
            {...register('type')}
            className="w-4 h-4 text-red-600"
          />
          <span className="text-red-600 font-medium">Expense</span>
        </label>
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="radio"
            value="Income"
            {...register('type')}
            className="w-4 h-4 text-green-600"
          />
          <span className="text-green-600 font-medium">Income</span>
        </label>
      </div>
      {errors.type && (
        <p className="text-red-500 text-sm">{errors.type.message}</p>
      )}

      {/* Description */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <Input
          {...register('description')}
          placeholder="e.g., Grocery shopping"
          error={errors.description?.message}
        />
      </div>

      {/* Amount */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Amount
        </label>
        <Input
          type="number"
          step="0.01"
          {...register('amount', { valueAsNumber: true })}
          placeholder="0.00"
          error={errors.amount?.message}
        />
      </div>

      {/* Date */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Date
        </label>
        <Input
          type="date"
          {...register('date')}
          error={errors.date?.message}
        />
      </div>

      {/* Category */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Category
        </label>
        <select
          {...register('categoryId')}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
        >
          <option value="">Select a category</option>
          {categories?.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      {/* Submit Button */}
      <Button
        type="submit"
        isLoading={isLoading}
        className={`w-full ${
          transactionType === 'Income'
            ? 'bg-green-600 hover:bg-green-700'
            : 'bg-red-600 hover:bg-red-700'
        }`}
      >
        {defaultValues ? 'Update' : 'Add'} {transactionType}
      </Button>
    </form>
  );
}
```

---

## 7. Chart.js Components

### Chart.js Registration

```typescript
// src/components/charts/chartConfig.ts
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

// Default chart options
export const defaultOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom' as const,
    },
  },
};
```

### Spending by Category Pie Chart

```tsx
// src/components/charts/SpendingPieChart.tsx
import { Pie } from 'react-chartjs-2';
import { useCategoryBreakdown } from '@/hooks/useDashboard';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { startOfMonth, endOfMonth, format } from 'date-fns';

const COLORS = [
  '#3B82F6', // blue
  '#EF4444', // red
  '#10B981', // green
  '#F59E0B', // yellow
  '#8B5CF6', // purple
  '#EC4899', // pink
  '#06B6D4', // cyan
  '#F97316', // orange
];

export function SpendingPieChart() {
  const now = new Date();
  const startDate = format(startOfMonth(now), 'yyyy-MM-dd');
  const endDate = format(endOfMonth(now), 'yyyy-MM-dd');

  const { data, isLoading, error } = useCategoryBreakdown(startDate, endDate);

  if (isLoading) return <Loading />;
  if (error) return <div className="text-red-500">Failed to load data</div>;
  if (!data || data.length === 0) {
    return (
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4">Spending by Category</h3>
        <p className="text-gray-500 text-center py-8">No spending data available</p>
      </Card>
    );
  }

  const chartData = {
    labels: data.map((item) => item.categoryName),
    datasets: [
      {
        data: data.map((item) => item.total),
        backgroundColor: COLORS.slice(0, data.length),
        borderWidth: 0,
      },
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'right' as const,
        labels: {
          usePointStyle: true,
          padding: 16,
        },
      },
      tooltip: {
        callbacks: {
          label: (context: any) => {
            const value = context.raw as number;
            const total = data.reduce((sum, item) => sum + item.total, 0);
            const percentage = ((value / total) * 100).toFixed(1);
            return `$${value.toLocaleString()} (${percentage}%)`;
          },
        },
      },
    },
  };

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold mb-4">Spending by Category</h3>
      <div className="h-64">
        <Pie data={chartData} options={options} />
      </div>
    </Card>
  );
}
```

### Income vs Expenses Line Chart

```tsx
// src/components/charts/IncomeExpenseChart.tsx
import { Line } from 'react-chartjs-2';
import { useIncomeVsExpenses } from '@/hooks/useDashboard';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';

export function IncomeExpenseChart() {
  const { data, isLoading, error } = useIncomeVsExpenses(6);

  if (isLoading) return <Loading />;
  if (error) return <div className="text-red-500">Failed to load data</div>;
  if (!data || data.length === 0) {
    return (
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4">Income vs Expenses</h3>
        <p className="text-gray-500 text-center py-8">No data available</p>
      </Card>
    );
  }

  const chartData = {
    labels: data.map((item) => item.month),
    datasets: [
      {
        label: 'Income',
        data: data.map((item) => item.income),
        borderColor: '#10B981',
        backgroundColor: 'rgba(16, 185, 129, 0.1)',
        fill: true,
        tension: 0.4,
      },
      {
        label: 'Expenses',
        data: data.map((item) => item.expenses),
        borderColor: '#EF4444',
        backgroundColor: 'rgba(239, 68, 68, 0.1)',
        fill: true,
        tension: 0.4,
      },
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index' as const,
    },
    plugins: {
      legend: {
        position: 'top' as const,
      },
      tooltip: {
        callbacks: {
          label: (context: any) => {
            return `${context.dataset.label}: $${context.raw.toLocaleString()}`;
          },
        },
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value: number) => `$${value.toLocaleString()}`,
        },
      },
    },
  };

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold mb-4">Income vs Expenses</h3>
      <div className="h-80">
        <Line data={chartData} options={options} />
      </div>
    </Card>
  );
}
```

### Budget Progress Bar Chart

```tsx
// src/components/charts/BudgetProgressChart.tsx
import { Bar } from 'react-chartjs-2';
import { useBudgets } from '@/hooks/useBudgets';
import { Card } from '@/components/ui/Card';

export function BudgetProgressChart() {
  const { data: budgets } = useBudgets();

  if (!budgets || budgets.length === 0) return null;

  const chartData = {
    labels: budgets.map((b) => b.name),
    datasets: [
      {
        label: 'Spent',
        data: budgets.map((b) => b.spent),
        backgroundColor: budgets.map((b) =>
          b.percentageUsed > 90 ? '#EF4444' : 
          b.percentageUsed > 70 ? '#F59E0B' : '#10B981'
        ),
        borderRadius: 4,
      },
      {
        label: 'Remaining',
        data: budgets.map((b) => Math.max(0, b.amount - b.spent)),
        backgroundColor: '#E5E7EB',
        borderRadius: 4,
      },
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y' as const,
    scales: {
      x: {
        stacked: true,
        ticks: {
          callback: (value: number) => `$${value}`,
        },
      },
      y: {
        stacked: true,
      },
    },
    plugins: {
      tooltip: {
        callbacks: {
          label: (context: any) => `${context.dataset.label}: $${context.raw}`,
        },
      },
    },
  };

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold mb-4">Budget Progress</h3>
      <div className="h-64">
        <Bar data={chartData} options={options} />
      </div>
    </Card>
  );
}
```

---

## 8. Dashboard Page

```tsx
// src/features/dashboard/Dashboard.tsx
import { useDashboardSummary } from '@/hooks/useDashboard';
import { OverviewCards } from './OverviewCards';
import { RecentTransactions } from './RecentTransactions';
import { SpendingPieChart } from '@/components/charts/SpendingPieChart';
import { IncomeExpenseChart } from '@/components/charts/IncomeExpenseChart';
import { BudgetProgressChart } from '@/components/charts/BudgetProgressChart';
import { Loading } from '@/components/ui/Loading';

export function Dashboard() {
  const { data: summary, isLoading } = useDashboardSummary();

  if (isLoading) return <Loading fullScreen />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Overview</h1>

      {/* Summary Cards */}
      <OverviewCards
        totalBalance={summary?.totalBalance ?? 0}
        monthlyIncome={summary?.monthlyIncome ?? 0}
        monthlyExpenses={summary?.monthlyExpenses ?? 0}
      />

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <SpendingPieChart />
        <IncomeExpenseChart />
      </div>

      {/* Budget Progress */}
      <BudgetProgressChart />

      {/* Recent Transactions */}
      <RecentTransactions />
    </div>
  );
}
```

### Overview Cards Component

```tsx
// src/features/dashboard/OverviewCards.tsx
import { TrendingUp, TrendingDown, Wallet } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { formatCurrency } from '@/utils/formatters';

interface OverviewCardsProps {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
}

export function OverviewCards({
  totalBalance,
  monthlyIncome,
  monthlyExpenses,
}: OverviewCardsProps) {
  const cards = [
    {
      title: 'Total Balance',
      value: totalBalance,
      icon: Wallet,
      color: 'bg-blue-500',
      textColor: 'text-blue-600',
    },
    {
      title: 'Monthly Income',
      value: monthlyIncome,
      icon: TrendingUp,
      color: 'bg-green-500',
      textColor: 'text-green-600',
    },
    {
      title: 'Monthly Expenses',
      value: monthlyExpenses,
      icon: TrendingDown,
      color: 'bg-red-500',
      textColor: 'text-red-600',
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
      {cards.map((card) => (
        <Card key={card.title} className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">{card.title}</p>
              <p className={`text-2xl font-bold ${card.textColor}`}>
                {formatCurrency(card.value)}
              </p>
            </div>
            <div className={`p-3 rounded-full ${card.color}`}>
              <card.icon className="w-6 h-6 text-white" />
            </div>
          </div>
        </Card>
      ))}
    </div>
  );
}
```

---

## 9. TypeScript Types

```typescript
// src/types/transaction.ts
export type TransactionType = 'Income' | 'Expense';

export interface Transaction {
  id: string;
  description: string;
  amount: number;
  type: TransactionType;
  date: string;
  categoryId?: string;
  categoryName?: string;
  createdAt: string;
}

export interface CreateTransactionRequest {
  description: string;
  amount: number;
  type: TransactionType;
  date: string;
  categoryId?: string;
}

export interface UpdateTransactionRequest {
  description: string;
  amount: number;
  date: string;
  categoryId?: string;
}

export interface TransactionQueryParams {
  startDate?: string;
  endDate?: string;
  categoryId?: string;
  type?: TransactionType;
  page?: number;
  pageSize?: number;
}

// src/types/api.ts
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DashboardSummary {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
}

export interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  total: number;
  transactionCount: number;
}
```

---

## 10. Utility Functions

```typescript
// src/utils/formatters.ts
export function formatCurrency(
  amount: number,
  currency: string = 'USD',
  locale: string = 'en-US'
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
  }).format(amount);
}

export function formatDate(
  date: string | Date,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }
): string {
  return new Date(date).toLocaleDateString('en-US', options);
}

export function formatPercentage(value: number, decimals: number = 1): string {
  return `${value.toFixed(decimals)}%`;
}

// src/utils/dateUtils.ts
import { 
  startOfMonth, 
  endOfMonth, 
  subMonths, 
  format 
} from 'date-fns';

export function getCurrentMonthRange() {
  const now = new Date();
  return {
    startDate: format(startOfMonth(now), 'yyyy-MM-dd'),
    endDate: format(endOfMonth(now), 'yyyy-MM-dd'),
  };
}

export function getLastNMonthsRange(n: number) {
  const now = new Date();
  return {
    startDate: format(startOfMonth(subMonths(now, n - 1)), 'yyyy-MM-dd'),
    endDate: format(endOfMonth(now), 'yyyy-MM-dd'),
  };
}
```

---

## 11. Environment Configuration

```bash
# .env.example
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=Personal Finance Tracker
```

```bash
# .env.production
VITE_API_URL=https://your-api.azurewebsites.net/api
VITE_APP_NAME=Personal Finance Tracker
```

---

## 12. References

- [React Documentation](https://react.dev/)
- [TanStack Query](https://tanstack.com/query/latest)
- [React Hook Form](https://react-hook-form.com/)
- [Chart.js](https://www.chartjs.org/docs/latest/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Vite](https://vitejs.dev/)
- [Zod](https://zod.dev/)

---

*Previous: [02-Backend-Documentation.md](./02-Backend-Documentation.md)*  
*Next: [04-DevOps-Deployment.md](./04-DevOps-Deployment.md)*
