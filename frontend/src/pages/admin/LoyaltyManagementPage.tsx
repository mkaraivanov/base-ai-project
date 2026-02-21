import React, { useEffect, useState } from 'react';
import { Gift } from 'lucide-react';
import { toast } from 'sonner';
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
        toast.error(extractErrorMessage(err, 'Failed to load loyalty settings'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    const parsed = parseInt(inputValue, 10);
    if (isNaN(parsed) || parsed < 1) { toast.error('Number of visits must be at least 1'); return; }
    setSaving(true);
    try {
      const updated = await loyaltyApi.updateSettings({ stampsRequired: parsed });
      setStampsRequired(updated.stampsRequired);
      setInputValue(String(updated.stampsRequired));
      toast.success('Loyalty settings saved.');
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err, 'Failed to save loyalty settings'));
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
            <Typography variant="h5" fontWeight={700}>Loyalty Program</Typography>
            <Typography variant="body2" color="text.secondary">Configure the free ticket reward threshold.</Typography>
          </Box>
        </Box>

        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3, mb: 3 }}>
          {loading ? (
            <MuiSkeleton height={56} sx={{ borderRadius: 2, mb: 2 }} />
          ) : (
            <TextField
              label="Visits required for a free ticket"
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
            Currently set to <strong>{stampsRequired}</strong> visits.
          </Typography>
          <MuiButton
            variant="contained"
            fullWidth
            onClick={handleSave}
            disabled={saving || loading}
          >
            {saving ? 'Saving…' : 'Save Settings'}
          </MuiButton>
        </Paper>

        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
          <Typography fontWeight={600} mb={2}>How it works</Typography>
          <List dense disablePadding sx={{ '& li': { pl: 0 } }}>
            {[
              'Each qualifying booking awards the customer 1 stamp.',
              'Multiple tickets in one transaction count as a single visit.',
              'When a customer reaches the required stamps, they receive a free ticket voucher.',
              'The stamp counter resets after each reward is issued.',
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
