import { z } from 'zod';

export const categorySchema = z.object({
  name: z
    .string()
    .min(1, 'Category name is required.')
    .max(100, 'Category name cannot exceed 100 characters.'),
  icon: z
    .string()
    .max(50, 'Icon cannot exceed 50 characters.')
    .optional()
    .or(z.literal('')),
  color: z
    .string()
    .max(20, 'Color cannot exceed 20 characters.')
    .optional()
    .or(z.literal('')),
});

export type CategoryFormData = z.infer<typeof categorySchema>;