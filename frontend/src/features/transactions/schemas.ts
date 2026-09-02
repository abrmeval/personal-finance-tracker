import { z } from 'zod';

export const transactionSchema = z.object({
  description: z
    .string()
    .min(1, 'Description is required.')
    .max(500, 'Description cannot exceed 500 characters.'),
  amount: z.coerce.number().positive('Amount must be greater than zero.'),
  type: z.enum(['Income', 'Expense'] as const),
  date: z.string().min(1, 'Date is required.'),
  categoryId: z.string().optional().or(z.literal('')),
  notes: z
    .string()
    .max(2000, 'Notes cannot exceed 2000 characters.')
    .optional()
    .or(z.literal('')),
});

export type TransactionFormData = z.infer<typeof transactionSchema>;
export type TransactionFormInput = z.input<typeof transactionSchema>;