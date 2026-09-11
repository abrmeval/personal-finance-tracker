import { z } from 'zod';

export const budgetSchema = z.object({
  categoryId: z.string().min(1, 'Please select a category.'),
  name: z
    .string()
    .min(1, 'Budget name is required.')
    .max(150, 'Budget name cannot exceed 150 characters.'),
  limitAmount: z.coerce
    .number()
    .positive('Limit amount must be greater than zero.'),
  period: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly'] as const),
});

export type BudgetFormData = z.infer<typeof budgetSchema>;
export type BudgetFormInput = z.input<typeof budgetSchema>;
