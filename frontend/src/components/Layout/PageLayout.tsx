import React from 'react';
import Container from '@mui/material/Container';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface PageLayoutProps {
  children: React.ReactNode;
  /** Remove horizontal padding/max-width constraint for full-bleed sections */
  fullBleed?: boolean;
  sx?: object;
}

export function PageLayout({ children, fullBleed, sx }: PageLayoutProps) {
  if (fullBleed) {
    return (
      <Box sx={{ flex: 1, ...sx }}>
        {children}
      </Box>
    );
  }
  return (
    <Container maxWidth="lg" sx={{ flex: 1, py: 3, ...sx }}>
      {children}
    </Container>
  );
}

interface SectionProps {
  children: React.ReactNode;
  title?: string;
  description?: string;
  action?: React.ReactNode;
  sx?: object;
}

export function Section({ children, title, description, action, sx }: SectionProps) {
  return (
    <Box component="section" sx={{ mb: 4, ...sx }}>
      {(title || action) && (
        <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 2, mb: 2 }}>
          <Box>
            {title && (
              <Typography variant="h5" fontWeight={700}>{title}</Typography>
            )}
            {description && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{description}</Typography>
            )}
          </Box>
          {action && <Box sx={{ flexShrink: 0 }}>{action}</Box>}
        </Box>
      )}
      {children}
    </Box>
  );
}
