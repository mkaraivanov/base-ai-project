import React, { useEffect, useState } from 'react';
import { Gift } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import TextField from '@mui/material/TextField';
import MuiButton from '@mui/material/Button';
import MuiSkeleton from '@mui/material/Skeleton';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemText from '@mui/material/ListItemText';
import { loyaltyApi } from '../../api/loyaltyApi';
import { extractErrorMessage } from '../../utils/errorHandler';

export const LoyaltyManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [stampsRequired, setStampsRequired] = useState<number>(5);
  const [inputValue, setInputValue] = useState<string>('5');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const load = async () => {
      try {
        const settings = await loyaltyApi.getSettings();
        setStampsRequired(settings.stampsRequired);
        setInputValue(String(settings.stampsRequired));
      } catch (err: unknown) {
        toast.error(extractErrorMessage(err, t('loyalty.failedToLoad')));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [t]);

  const handleSave = async () => {
    const parsed = parseInt(inputValue, 10);
    if (isNaN(parsed) || parsed < 1) { toast.error(t('loyalty.invalidVisits')); return; }
    setSaving(true);
    try {
      const updated = await loyaltyApi.updateSettings({ stampsRequired: parsed });
      setStampsRequired(updated.stampsRequired);
      setInputValue(String(updated.stampsRequired));
      toast.success(t('loyalty.savedSuccess'));
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err, t('loyalty.failedToSave')));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="sm" sx={{ py: 5 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 4 }}>
          <Box sx={{ width: 40, height: 40, borderRadius: 2, bgcolor: 'rgba(20,184,166,0.1)', color: '#14b8a6', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Gift size={20} />
          </Box>
          <Box>
            <Typography variant="h5" fontWeight={700}>{t('loyalty.title')}</Typography>
            <Typography variant="body2" color="text.secondary">{t('loyalty.pageSubtitle')}</Typography>
          </Box>
        </Box>

        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3, mb: 3 }}>
          {loading ? (
            <MuiSkeleton height={56} sx={{ borderRadius: 2, mb: 2 }} />
          ) : (
            <TextField
              label={t('loyalty.visitsRequired')}
              type="number"
              value={inputValue}
              onChange={e => setInputValue(e.target.value)}
              inputProps={{ min: 1, max: 100 }}
              fullWidth
              size="small"
              sx={{ mb: 1.5 }}
            />
          )}
          <Typography variant="caption" color="text.secondary" display="block" mb={2}>
            {t('loyalty.currentlySet', { count: stampsRequired })}
          </Typography>
          <MuiButton
            variant="contained"
            fullWidth
            onClick={handleSave}
            disabled={saving || loading}
          >
            {saving ? t('loyalty.saving') : t('loyalty.saveSettings')}
          </MuiButton>
        </Paper>

        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
          <Typography fontWeight={600} mb={2}>{t('loyalty.howItWorks')}</Typography>
          <List dense disablePadding sx={{ '& li': { pl: 0 } }}>
            {[
              t('loyalty.bullet1'),
              t('loyalty.bullet3'),
              t('loyalty.bullet4'),
              t('loyalty.bullet5'),
            ].map((text, i) => (
              <ListItem key={i} sx={{ px: 0, py: 0.5, alignItems: 'flex-start', gap: 1 }}>
                <Box sx={{ mt: 0.7, width: 6, height: 6, borderRadius: '50%', bgcolor: 'text.secondary', flexShrink: 0 }} />
                <ListItemText primary={text} primaryTypographyProps={{ variant: 'body2', color: 'text.secondary' }} />
              </ListItem>
            ))}
          </List>
        </Paper>
      </Container>
    </Box>
  );
};
