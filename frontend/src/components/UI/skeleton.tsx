import * as React from 'react';
import MuiSkeleton from '@mui/material/Skeleton';
import { type SxProps, type Theme } from '@mui/material/styles';

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  sx?: SxProps<Theme>;
  variant?: 'text' | 'rectangular' | 'rounded' | 'circular';
  width?: number | string;
  height?: number | string;
}

function Skeleton({ variant = 'rounded', width, height, sx, style, ...rest }: SkeletonProps) {
  return (
    <MuiSkeleton
      variant={variant}
      width={width ?? (style?.width as string | undefined)}
      height={height ?? (style?.height as string | undefined)}
      sx={sx}
      {...(rest as object)}
    />
  );
}

export { Skeleton };
