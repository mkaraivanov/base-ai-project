import * as React from 'react';
import MuiDialog from '@mui/material/Dialog';
import MuiDialogTitle from '@mui/material/DialogTitle';
import MuiDialogContent from '@mui/material/DialogContent';
import MuiDialogActions from '@mui/material/DialogActions';
import IconButton from '@mui/material/IconButton';
import { X } from 'lucide-react';

interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: React.ReactNode;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
}

function Dialog({ open, onOpenChange, children, maxWidth = 'sm' }: DialogProps) {
  return (
    <MuiDialog open={open} onClose={() => onOpenChange(false)} maxWidth={maxWidth} fullWidth>
      {children}
    </MuiDialog>
  );
}

interface DialogContentProps extends React.HTMLAttributes<HTMLDivElement> {
  onClose?: () => void;
}

const DialogContent = React.forwardRef<HTMLDivElement, DialogContentProps>(
  ({ children, onClose, ...rest }, _ref) => (
    <MuiDialogContent {...(rest as object)}>
      {onClose && (
        <IconButton onClick={onClose} size="small" sx={{ position: 'absolute', right: 8, top: 8 }}>
          <X size={16} />
        </IconButton>
      )}
      {children}
    </MuiDialogContent>
  )
);
DialogContent.displayName = 'DialogContent';

const DialogHeader = ({ children, ...rest }: React.HTMLAttributes<HTMLDivElement>) => (
  <MuiDialogTitle {...(rest as object)}>{children}</MuiDialogTitle>
);

const DialogTitle = ({ children, ...rest }: React.HTMLAttributes<HTMLHeadingElement>) => (
  <MuiDialogTitle component="h2" {...(rest as object)}>{children}</MuiDialogTitle>
);

const DialogDescription = ({ children, ...rest }: React.HTMLAttributes<HTMLParagraphElement>) => (
  <MuiDialogContent sx={{ color: 'text.secondary', typography: 'body2', pt: 0 }} {...(rest as object)}>
    {children}
  </MuiDialogContent>
);

const DialogFooter = ({ children, ...rest }: React.HTMLAttributes<HTMLDivElement>) => (
  <MuiDialogActions {...(rest as object)}>{children}</MuiDialogActions>
);

export { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter };
