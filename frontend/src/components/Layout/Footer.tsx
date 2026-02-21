import React from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { Film } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export const Footer: React.FC = () => {
  const { t } = useTranslation('common');
  return (
    <Box
      component="footer"
      sx={{
        borderTop: 1,
        borderColor: 'divider',
        bgcolor: 'background.paper',
        py: 4,
        mt: 'auto',
        display: { xs: 'none', md: 'block' },
      }}
    >
      <Box
        sx={{
          maxWidth: 1280,
          mx: 'auto',
          px: 3,
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 2,
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Box
            sx={{
              width: 24,
              height: 24,
              borderRadius: 1,
              bgcolor: 'primary.main',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
            }}
          >
            <Film size={12} />
          </Box>
          <Typography variant="body2" fontWeight={500}>CineBook</Typography>
          <Typography variant="body2" color="text.secondary">— {t('footer.tagline')}</Typography>
        </Box>
        <Typography variant="caption" color="text.secondary">
          {t('footer.copyright', { year: new Date().getFullYear() })}
        </Typography>
      </Box>
    </Box>
  );
};

