import { Pencil, Trash2 } from 'lucide-react';
import type { Category } from '@/types/finance';

interface CategoryListProps {
  categories: Category[];
  isLoading: boolean;
  error: Error | null;
  onEdit: (category: Category) => void;
  onDelete: (category: Category) => void;
}

export function CategoryList({ categories, isLoading, error, onEdit, onDelete }: CategoryListProps) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map(i => (
          <div key={i} className="h-16 rounded-lg bg-gray-100 animate-pulse" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
        Failed to load categories. Please try again.
      </div>
    );
  }

  if (categories.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500 text-sm">
        No categories yet. Create your first category to start organizing transactions.
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {categories.map(category => (
        <div
          key={category.id}
          className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-4 py-3"
        >
          <div className="flex items-center gap-3">
            {category.color && (
              <span
                className="inline-block h-3 w-3 rounded-full"
                style={{ backgroundColor: category.color }}
              />
            )}
            <div>
              <p className="text-sm font-medium text-gray-900">{category.name}</p>
              {category.icon && (
                <p className="text-xs text-gray-500">{category.icon}</p>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => onEdit(category)}
              className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
              aria-label={`Edit ${category.name}`}
            >
              <Pencil className="h-4 w-4" />
            </button>
            <button
              onClick={() => onDelete(category)}
              className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
              aria-label={`Delete ${category.name}`}
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}