import type { ApiResponse } from '../types';

/**
 * Extracts error message(s) from an API error response
 * @param error - The error object from a failed API call
 * @param defaultMessage - Default message if no specific error is found
 * @returns A formatted error message string
 */
export function extractErrorMessage(error: unknown, defaultMessage = 'An error occurred'): string {
  if (!error || typeof error !== 'object') {
    return defaultMessage;
  }

  const err = error as any;

  // Check for axios error response
  if (err.response?.data) {
    const data = err.response.data as Partial<ApiResponse<unknown>>;

    // If there are validation errors, join them with newlines
    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors.join('\n');
    }

    // If there's a single error message, return it
    if (data.error && typeof data.error === 'string') {
      return data.error;
    }

    // Legacy: check for message field
    if (data.message && typeof data.message === 'string') {
      return data.message;
    }

    // If data itself is a string, return it
    if (typeof data === 'string') {
      return data;
    }
  }

  // Check for error.message
  if (err.message && typeof err.message === 'string') {
    return err.message;
  }

  return defaultMessage;
}
