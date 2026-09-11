import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { budgetSchema } from '@/features/budgets/schemas';
import type {
  BudgetFormData,
  BudgetFormInput,
} from '@/features/budgets/schemas';
import type { BudgetPeriod } from '@/types/finance';
import { useCategories } from '@/features/categories/hooks/useCategories';

interface BudgetFormProps {
  defaultValues?: Partial<BudgetFormData>;
  onSubmit: (data: BudgetFormData) => void;
  isSubmitting: boolean;
  submitLabel?: string;
  modelErrors?: Record<string, string[]> | null;
}

const PERIOD_OPTIONS: { value: BudgetPeriod; label: string }[] = [
  { value: 'Daily', label: 'Daily' },
  { value: 'Weekly', label: 'Weekly' },
  { value: 'Monthly', label: 'Monthly' },
  { value: 'Yearly', label: 'Yearly' },
];

export function BudgetForm({
  defaultValues,
  onSubmit,
  isSubmitting,
  submitLabel = 'Save Budget',
  modelErrors,
}: BudgetFormProps) {
  const { data: categoriesResponse } = useCategories();
  const categories = categoriesResponse?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<BudgetFormInput, unknown, BudgetFormData>({
    resolver: zodResolver(budgetSchema),
    defaultValues: {
      categoryId: '',
      name: '',
      limitAmount: 0,
      period: 'Monthly',
      ...defaultValues,
    },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
      <div>
        <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700 mb-1">
          Category
        </label>
        <Controller
          control={control}
          name="categoryId"
          render={({ field }) => (
            <select
              id="categoryId"
              {...field}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
            >
              <option value="">Select a category…</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          )}
        />
        {errors.categoryId && (
          <p className="mt-1 text-xs text-red-600">{errors.categoryId.message}</p>
        )}
        {modelErrors?.categoryId &&
          modelErrors.categoryId.map((msg, idx) => (
            <p key={idx} className="text-xs text-red-600">
              {msg}
            </p>
          ))}
      </div>

      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Budget Name
        </label>
        <input
          id="name"
          type="text"
          {...register('name')}
          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
          placeholder="e.g., Monthly groceries"
        />
        {errors.name && (
          <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>
        )}
        {modelErrors?.name &&
          modelErrors.name.map((msg, idx) => (
            <p key={idx} className="text-xs text-red-600">
              {msg}
            </p>
          ))}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label htmlFor="limitAmount" className="block text-sm font-medium text-gray-700 mb-1">
            Limit Amount
          </label>
          <input
            id="limitAmount"
            type="number"
            step="0.01"
            {...register('limitAmount')}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
            placeholder="0.00"
          />
          {errors.limitAmount && (
            <p className="mt-1 text-xs text-red-600">{errors.limitAmount.message}</p>
          )}
          {modelErrors?.limitAmount &&
            modelErrors.limitAmount.map((msg, idx) => (
              <p key={idx} className="text-xs text-red-600">
                {msg}
              </p>
            ))}
        </div>

        <div>
          <label htmlFor="period" className="block text-sm font-medium text-gray-700 mb-1">
            Period
          </label>
          <select
            id="period"
            {...register('period')}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
          >
            {PERIOD_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
          {errors.period && (
            <p className="mt-1 text-xs text-red-600">{errors.period.message}</p>
          )}
          {modelErrors?.period &&
            modelErrors.period.map((msg, idx) => (
              <p key={idx} className="text-xs text-red-600">
                {msg}
              </p>
            ))}
        </div>
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  );
}
