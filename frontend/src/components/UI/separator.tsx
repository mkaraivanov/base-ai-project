import * as React from 'react';
import Divider from '@mui/material/Divider';

interface SeparatorProps extends React.HTMLAttributes<HTMLDivElement> {
  orientation?: 'horizontal' | 'vertical';
}

const Separator = React.forwardRef<HTMLDivElement, SeparatorProps>(
  ({ orientation = 'horizontal', ...rest }, _ref) => (
    <Divider orientation={orientation} flexItem={orientation === 'vertical'} {...(rest as object)} />
  )
);
Separator.displayName = 'Separator';

export { Separator };
