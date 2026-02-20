import * as React from 'react';
import LinearProgress from '@mui/material/LinearProgress';
import { type SxProps, type Theme } from '@mui/material/styles';

interface ProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  value?: number;
  max?: number;
  sx?: SxProps<Theme>;
}

const Progress = React.forwardRef<HTMLDivElement, ProgressProps>(
  ({ value = 0, max = 100, sx, ...rest }, _ref) => {
    const pct = Math.min(100, Math.max(0, (value / max) * 100));
    return (
      <LinearProgress
        variant="determinate"
        value={pct}
        sx={{ borderRadius: 1, ...sx }}
        {...(rest as object)}
      />
    );
  }
);
Progress.displayName = 'Progress';

export { Progress };
