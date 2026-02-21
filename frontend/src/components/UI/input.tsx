import * as React from 'react';
import TextField, { type TextFieldProps } from '@mui/material/TextField';

export interface InputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label?: string;
  helperText?: string;
  error?: boolean;
  fullWidth?: boolean;
  muiProps?: Partial<TextFieldProps>;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({
    label, helperText, error, fullWidth = true, type, placeholder,
    disabled, value, onChange, defaultValue, required, muiProps, id, name,
  }, ref) => {
    return (
      <TextField
        inputRef={ref}
        label={label}
        helperText={helperText}
        error={error}
        fullWidth={fullWidth}
        type={type}
        placeholder={placeholder}
        disabled={disabled}
        value={value}
        onChange={onChange as React.ChangeEventHandler<HTMLInputElement>}
        defaultValue={defaultValue}
        required={required}
        id={id}
        name={name}
        size="small"
        {...muiProps}
      />
    );
  }
);
Input.displayName = 'Input';
