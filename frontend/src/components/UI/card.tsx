import * as React from 'react';
import MuiCard from '@mui/material/Card';
import MuiCardContent from '@mui/material/CardContent';
import MuiCardHeader from '@mui/material/CardHeader';
import MuiCardActions from '@mui/material/CardActions';
import Typography from '@mui/material/Typography';
import { type SxProps, type Theme } from '@mui/material/styles';

interface CardProps extends React.HTMLAttributes<HTMLDivElement> { sx?: SxProps<Theme> }

const Card = React.forwardRef<HTMLDivElement, CardProps>(({ children, sx, ...rest }, ref) => (
  <MuiCard ref={ref} sx={sx} {...(rest as object)}>{children}</MuiCard>
));
Card.displayName = 'Card';

const CardHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement> & { sx?: SxProps<Theme> }>(
  ({ children, sx, ...rest }, ref) => (
    <MuiCardHeader ref={ref} sx={{ pb: 0, ...sx }} title={children} {...(rest as object)} />
  )
);
CardHeader.displayName = 'CardHeader';

const CardTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ children, ...rest }, ref) => (
    <Typography ref={ref} variant="h6" component="h3" fontWeight={600} {...(rest as object)}>{children}</Typography>
  )
);
CardTitle.displayName = 'CardTitle';

const CardDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ children, ...rest }, ref) => (
    <Typography ref={ref} variant="body2" color="text.secondary" {...(rest as object)}>{children}</Typography>
  )
);
CardDescription.displayName = 'CardDescription';

const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement> & { sx?: SxProps<Theme> }>(
  ({ children, sx, ...rest }, ref) => (
    <MuiCardContent ref={ref} sx={sx} {...(rest as object)}>{children}</MuiCardContent>
  )
);
CardContent.displayName = 'CardContent';

const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement> & { sx?: SxProps<Theme> }>(
  ({ children, sx, ...rest }, ref) => (
    <MuiCardActions ref={ref} sx={sx} {...(rest as object)}>{children}</MuiCardActions>
  )
);
CardFooter.displayName = 'CardFooter';

export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
