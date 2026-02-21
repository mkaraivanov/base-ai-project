import * as React from 'react';
import MuiDialog from '@mui/material/Dialog';
import MuiDialogTitle from '@mui/material/DialogTitle';
import MuiDialogContent from '@mui/material/DialogContent';
import MuiDialogActions from '@mui/material/DialogActions';
import MuiButton from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Box from '@mui/material/Box';
import { AlertTriangle } from 'lucide-react';

interface AlertDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  variant?: 'destructive' | 'default';
}

function AlertDialog({
  open, onOpenChange, title, description,
  confirmLabel = 'Confirm', cancelLabel = 'Cancel',
  onConfirm, variant = 'default',
}: AlertDialogProps) {
  return (
    <MuiDialog open={open} onClose={() => onOpenChange(false)} maxWidth="xs" fullWidth>
      <MuiDialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          {variant === 'destructive' && <AlertTriangle size={20} color="var(--mui-palette-error-main)" />}
          {title}
        </Box>
      </MuiDialogTitle>
      {description && (
        <MuiDialogContent>
          <Typography variant="body2" color="text.secondary">{description}</Typography>
        </MuiDialogContent>
      )}
      <MuiDialogActions>
        <MuiButton variant="outlined" color="inherit" onClick={() => onOpenChange(false)}>{cancelLabel}</MuiButton>
        <MuiButton
          variant="contained"
          color={variant === 'destructive' ? 'error' : 'primary'}
          onClick={() => { onConfirm(); onOpenChange(false); }}
        >
          {confirmLabel}
        </MuiButton>
      </MuiDialogActions>
    </MuiDialog>
  );
}

export { AlertDialog };
