import { z } from 'zod';
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required.')
    .max(256, 'Email cannot exceed 256 characters.')
    .email('Email must be a valid email address.'),
  password: z
    .string()
    .min(1, 'Password is required.'),
});
export const registerSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required.')
    .max(256, 'Email cannot exceed 256 characters.')
    .email('Email must be a valid email address.'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters.')
    .max(100, 'Password cannot exceed 100 characters.')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
    .regex(/[0-9]/, 'Password must contain at least one digit.')
    .regex(/[.,&()-*]/, 'Password must contain at least one special character (.,&()-*).'),
  confirmPassword: z
    .string()
    .min(1, 'Please confirm your password.'),
  firstName: z
    .string()
    .min(1, 'First name is required.')
    .max(100, 'First name cannot exceed 100 characters.')
    .regex(/^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$/, 'First name contains invalid characters.'),
  lastName: z
    .string()
    .min(1, 'Last name is required.')
    .max(100, 'Last name cannot exceed 100 characters.')
    .regex(/^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$/, 'Last name contains invalid characters.'),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Passwords do not match.',
  path: ['confirmPassword'],
});
export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;