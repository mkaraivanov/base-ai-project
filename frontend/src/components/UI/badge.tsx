import * as React from 'react';
import Chip, { type ChipProps } from '@mui/material/Chip';

export interface BadgeProps {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning' | 'ghost';
  children?: React.ReactNode;
  className?: string;
  onClick?: () => void;
  sx?: ChipProps['sx'];
}

const variantSx: Record<string, ChipProps['sx']> = {
  default:     { bgcolor: 'primary.main',    color: 'primary.contrastText' },
  secondary:   { bgcolor: 'action.selected', color: 'text.primary' },
  destructive: { bgcolor: 'error.main',      color: 'white' },
  outline:     { bgcolor: 'transparent', border: 1, borderColor: 'divider', color: 'text.primary' },
  success:     { bgcolor: 'rgba(34,197,94,0.15)',  color: 'success.main' },
  warning:     { bgcolor: 'rgba(245,158,11,0.15)', color: 'warning.main' },
  ghost:       { bgcolor: 'primary.50',      color: 'primary.main' },
};

export function Badge({ variant = 'default', children, sx, onClick, ...rest }: BadgeProps) {
  return (
    <Chip
      label={children}
      size="small"
      onClick={onClick}
      sx={{ ...variantSx[variant], fontWeight: 600, borderRadius: '999px', ...sx }}
      {...rest}
    />
  );
}
