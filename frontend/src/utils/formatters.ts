import { format, formatDistanceToNow, parseISO } from 'date-fns';
import { enUS, bg } from 'date-fns/locale';
import i18n from '../i18n';

const DATE_FNS_LOCALES: Record<string, Locale> = {
  en: enUS,
  bg,
};

function getDateFnsLocale(): Locale {
  const lang = i18n.language?.split('-')[0] ?? 'en';
  return DATE_FNS_LOCALES[lang] ?? enUS;
}

export const formatDate = (dateString: string): string => {
  return format(parseISO(dateString), 'MMM d, yyyy', { locale: getDateFnsLocale() });
};

export const formatDateTime = (dateString: string): string => {
  return format(parseISO(dateString), 'MMM d, yyyy h:mm a', { locale: getDateFnsLocale() });
};

export const formatTime = (dateString: string): string => {
  return format(parseISO(dateString), 'h:mm a', { locale: getDateFnsLocale() });
};

export const formatRelativeTime = (dateString: string): string => {
  return formatDistanceToNow(parseISO(dateString), { addSuffix: true, locale: getDateFnsLocale() });
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
