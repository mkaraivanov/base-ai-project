import * as React from 'react';
import MuiButton, { type ButtonProps as MuiButtonProps } from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import { type SxProps, type Theme } from '@mui/material/styles';

export interface ButtonProps extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'color'> {
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  size?: 'default' | 'sm' | 'lg' | 'icon';
  asChild?: boolean;
  sx?: SxProps<Theme>;
}

const variantMap: Record<string, { muiVariant: MuiButtonProps['variant']; color: string; sx?: SxProps<Theme> }> = {
  default:     { muiVariant: 'contained', color: 'primary' },
  destructive: { muiVariant: 'contained', color: 'error' },
  outline:     { muiVariant: 'outlined',  color: 'primary' },
  secondary:   { muiVariant: 'outlined',  color: 'inherit' },
  ghost:       { muiVariant: 'text',      color: 'inherit' },
  link:        { muiVariant: 'text',      color: 'primary', sx: { p: 0, minWidth: 'auto', textDecoration: 'underline' } },
};

const sizeMap: Record<string, { muiSize: MuiButtonProps['size'] }> = {
  default: { muiSize: 'medium' },
  sm:      { muiSize: 'small'  },
  lg:      { muiSize: 'large'  },
  icon:    { muiSize: 'small'  },
};

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'default', size = 'default', disabled, onClick, children, type = 'button', sx, ...rest }, ref) => {
    const v = variantMap[variant] ?? variantMap.default;
    const s = sizeMap[size]    ?? sizeMap.default;

    if (size === 'icon') {
      return (
        <IconButton
          ref={ref}
          disabled={disabled}
          onClick={onClick as React.MouseEventHandler<HTMLButtonElement>}
          type={type}
          size="small"
          color={v.color as 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'}
          sx={sx}
          {...(rest as object)}
        >
          {children}
        </IconButton>
      );
    }

    return (
      <MuiButton
        ref={ref}
        variant={v.muiVariant}
        color={v.color as MuiButtonProps['color']}
        size={s.muiSize}
        disabled={disabled}
        onClick={onClick as React.MouseEventHandler<HTMLButtonElement>}
        type={type}
        sx={{ ...v.sx, ...sx }}
        {...(rest as object)}
      >
        {children}
      </MuiButton>
    );
  }
);
Button.displayName = 'Button';
