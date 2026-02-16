import { format, formatDistanceToNow, parseISO } from 'date-fns';

export const formatDate = (dateString: string): string => {
  return format(parseISO(dateString), 'MMM d, yyyy');
};

export const formatDateTime = (dateString: string): string => {
  return format(parseISO(dateString), 'MMM d, yyyy h:mm a');
};

export const formatTime = (dateString: string): string => {
  return format(parseISO(dateString), 'h:mm a');
};

export const formatRelativeTime = (dateString: string): string => {
  return formatDistanceToNow(parseISO(dateString), { addSuffix: true });
};

export const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
};

export const formatDuration = (minutes: number): string => {
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  if (hours === 0) return `${remainingMinutes}m`;
  if (remainingMinutes === 0) return `${hours}h`;
  return `${hours}h ${remainingMinutes}m`;
};
