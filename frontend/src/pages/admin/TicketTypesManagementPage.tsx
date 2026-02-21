import React, { useEffect, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Pencil, Trash2, X, Ticket } from 'lucide-react';
import { toast } from 'sonner';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import FormControlLabel from '@mui/material/FormControlLabel';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import { ticketTypeApi } from '../../api/ticketTypeApi';
import type { TicketTypeDto, CreateTicketTypeDto, UpdateTicketTypeDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';
import { Badge } from '../../components/ui/badge';
import { AlertDialog } from '../../components/ui/alert-dialog';

interface TicketTypeFormData { name: string; description: string; priceModifier: string; sortOrder: string; isActive: boolean; }
const EMPTY: TicketTypeFormData = { name: '', description: '', priceModifier: '1.00', sortOrder: '0', isActive: true };

function formatModifier(mod: number): string {
  if (isNaN(mod)) return '–';
  if (mod === 1.0) return '×1.00 (full price)';
  const pct = Math.round((mod - 1) * 100);
  const label = pct > 0 ? `+${pct}%` : `${pct}%`;
  return `×${mod.toFixed(2)} (${label})`;
}

export const TicketTypesManagementPage: React.FC = () => {
  const [ticketTypes, setTicketTypes] = useState<readonly TicketTypeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<TicketTypeFormData>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const load = async () => {
    try { setLoading(true); setTicketTypes(await ticketTypeApi.getAll()); }
    catch { toast.error('Failed to load ticket types'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const set = (name: keyof TicketTypeFormData, v: string | boolean) => setForm(p => ({ ...p, [name]: v }));

  const openCreate = () => { setEditingId(null); setForm(EMPTY); setShowForm(true); };
  const openEdit = (t: TicketTypeDto) => {
    setEditingId(t.id);
    setForm({ name: t.name, description: t.description ?? '', priceModifier: t.priceModifier.toString(), sortOrder: t.sortOrder.toString(), isActive: t.isActive });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setSaving(true);
    try {
      const payload = { name: form.name, description: form.description || null, priceModifier: parseFloat(form.priceModifier), sortOrder: parseInt(form.sortOrder, 10) };
      if (editingId) {
        await ticketTypeApi.update(editingId, { ...payload, isActive: form.isActive } as UpdateTicketTypeDto);
        toast.success('Ticket type updated.');
      } else {
        await ticketTypeApi.create(payload as CreateTicketTypeDto);
        toast.success('Ticket type created.');
      }
      setShowForm(false); setEditingId(null); await load();
    } catch (err) { toast.error(extractErrorMessage(err, 'Failed to save ticket type')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try { await ticketTypeApi.delete(deleteId); toast.success('Ticket type deleted.'); await load(); }
    catch (err) { toast.error(extractErrorMessage(err, 'Failed to delete ticket type')); }
    finally { setDeleteId(null); }
  };

  const modifierPreview = formatModifier(parseFloat(form.priceModifier));

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ p: 1, borderRadius: 2, bgcolor: 'rgba(16,185,129,0.1)', color: '#10b981' }}><Ticket size={24} /></Box>
            <Box>
              <Typography variant="h5" fontWeight={700}>Ticket Types</Typography>
              <Typography variant="body2" color="text.secondary">{ticketTypes.length} type{ticketTypes.length !== 1 ? 's' : ''}</Typography>
            </Box>
          </Box>
          <MuiButton variant="contained" startIcon={<Plus size={16} />} onClick={openCreate}>Add Ticket Type</MuiButton>
        </Box>

        <AnimatePresence>
          {showForm && (
            <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} style={{ marginBottom: 32 }}>
              <Paper variant="outlined" sx={{ borderRadius: 3, p: 3 }} component="form" onSubmit={handleSubmit}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography fontWeight={600}>{editingId ? 'Edit Ticket Type' : 'Add Ticket Type'}</Typography>
                  <IconButton size="small" onClick={() => setShowForm(false)}><X size={18} /></IconButton>
                </Box>
                <Grid container spacing={2}>
                  <Grid size={6}>
                    <TextField label="Name *" value={form.name} onChange={e => set('name', e.target.value)} required fullWidth size="small" placeholder="Student" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Sort Order" type="number" value={form.sortOrder} onChange={e => set('sortOrder', e.target.value)} fullWidth size="small" />
                  </Grid>
                  <Grid size={12}>
                    <TextField label="Description" value={form.description} onChange={e => set('description', e.target.value)} fullWidth multiline rows={2} size="small" />
                  </Grid>
                  <Grid size={12}>
                    <TextField
                      label="Price Modifier *"
                      type="number"
                      slotProps={{ htmlInput: { min: 0.01, max: 5, step: 0.01 } }}
                      value={form.priceModifier}
                      onChange={e => set('priceModifier', e.target.value)}
                      required fullWidth size="small"
                      helperText={`Preview: ${modifierPreview}`}
                    />
                  </Grid>
                  {editingId && (
                    <Grid size={12}>
                      <FormControlLabel control={<Checkbox checked={form.isActive} onChange={e => set('isActive', e.target.checked)} size="small" />} label="Active" />
                    </Grid>
                  )}
                </Grid>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1.5, mt: 3 }}>
                  <MuiButton variant="outlined" onClick={() => setShowForm(false)}>Cancel</MuiButton>
                  <MuiButton type="submit" variant="contained" disabled={saving}>{saving ? 'Saving…' : editingId ? 'Update' : 'Create'}</MuiButton>
                </Box>
              </Paper>
            </motion.div>
          )}
        </AnimatePresence>

        {loading ? (
          <Stack spacing={1}>{Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} height={48} sx={{ borderRadius: 2 }} />)}</Stack>
        ) : ticketTypes.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 10 }}>
            <Ticket size={48} color="rgba(128,128,128,0.3)" style={{ marginBottom: 16 }} />
            <Typography color="text.secondary">No ticket types yet.</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ '& th': { fontWeight: 600, bgcolor: 'action.hover' } }}>
                  <TableCell>Name</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell>Price Modifier</TableCell>
                  <TableCell>Sort</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {ticketTypes.map(t => (
                  <TableRow key={t.id} hover>
                    <TableCell><Typography variant="body2" fontWeight={500}>{t.name}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{t.description ?? '–'}</Typography></TableCell>
                    <TableCell>
                      <Chip label={formatModifier(t.priceModifier)} size="small" variant="outlined" sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }} />
                    </TableCell>
                    <TableCell><Typography variant="body2" fontFamily="monospace">{t.sortOrder}</Typography></TableCell>
                    <TableCell><Badge variant={t.isActive ? 'success' : 'secondary'}>{t.isActive ? 'Active' : 'Inactive'}</Badge></TableCell>
                    <TableCell align="right">
                      <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                        <Tooltip title="Edit"><IconButton size="small" onClick={() => openEdit(t)}><Pencil size={13} /></IconButton></Tooltip>
                        <Tooltip title="Delete"><IconButton size="small" onClick={() => setDeleteId(t.id)} sx={{ color: 'error.main' }}><Trash2 size={13} /></IconButton></Tooltip>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Container>

      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }} title="Delete Ticket Type?" description="This will permanently remove this ticket type." confirmLabel="Delete" variant="destructive" onConfirm={handleDelete} />
    </Box>
  );
};
