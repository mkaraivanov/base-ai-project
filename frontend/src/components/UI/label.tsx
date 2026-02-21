import * as React from 'react';
import FormLabel from '@mui/material/FormLabel';

const Label = React.forwardRef<HTMLLabelElement, React.LabelHTMLAttributes<HTMLLabelElement>>(
  ({ children, ...rest }, ref) => (
    <FormLabel ref={ref} {...rest}>{children}</FormLabel>
  )
);
Label.displayName = 'Label';

export { Label };
