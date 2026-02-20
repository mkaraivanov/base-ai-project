import * as React from 'react';
import Drawer from '@mui/material/Drawer';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Typography from '@mui/material/Typography';
import { X } from 'lucide-react';

interface SheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: React.ReactNode;
  side?: 'left' | 'right' | 'bottom' | 'top';
}

function Sheet({ open, onOpenChange, children, side = 'right' }: SheetProps) {
  return (
    <Drawer anchor={side} open={open} onClose={() => onOpenChange(false)}>
      <Box sx={{ width: side === 'bottom' || side === 'top' ? 'auto' : 320, minHeight: side === 'bottom' ? 200 : '100vh' }}>
        {children}
      </Box>
    </Drawer>
  );
}

const SheetContent = ({ children, ...rest }: React.HTMLAttributes<HTMLDivElement>) => (
  <Box sx={{ p: 3, height: '100%' }} {...(rest as object)}>{children}</Box>
);

const SheetHeader = ({ children, ...rest }: React.HTMLAttributes<HTMLDivElement>) => (
  <Box
    sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}
    {...(rest as object)}
  >
    {children}
  </Box>
);

const SheetTitle = ({ children, ...rest }: React.HTMLAttributes<HTMLHeadingElement>) => (
  <Typography variant="h6" fontWeight={600} {...(rest as object)}>{children}</Typography>
);

const SheetClose = ({ onClick }: { onClick?: () => void }) => (
  <IconButton size="small" onClick={onClick}><X size={18} /></IconButton>
);

export { Sheet, SheetContent, SheetHeader, SheetTitle, SheetClose };
