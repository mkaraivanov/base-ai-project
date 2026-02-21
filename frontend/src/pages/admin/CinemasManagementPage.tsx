import React, { useEffect, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Pencil, Trash2, X, Building2, MapPin, Clock, Phone, Mail } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
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
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto, CreateCinemaDto, UpdateCinemaDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';
import { Badge } from '../../components/ui/badge';
import { AlertDialog } from '../../components/ui/alert-dialog';

interface CinemaFormData {
  name: string; address: string; city: string; country: string;
  phoneNumber: string; email: string; logoUrl: string;
  openTime: string; closeTime: string; isActive: boolean;
}

const EMPTY: CinemaFormData = { name: '', address: '', city: '', country: '', phoneNumber: '', email: '', logoUrl: '', openTime: '09:00', closeTime: '23:00', isActive: true };

export const CinemasManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CinemaFormData>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const load = async () => {
    try { setLoading(true); setCinemas(await cinemaApi.getAll(false)); }
    catch { toast.error('Failed to load cinemas'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const set = (name: keyof CinemaFormData, value: string | boolean) =>
    setForm(prev => ({ ...prev, [name]: value }));

  const openCreate = () => { setEditingId(null); setForm(EMPTY); setShowForm(true); };
  const openEdit = (c: CinemaDto) => {
    setEditingId(c.id);
    setForm({ name: c.name, address: c.address, city: c.city, country: c.country, phoneNumber: c.phoneNumber ?? '', email: c.email ?? '', logoUrl: c.logoUrl ?? '', openTime: c.openTime, closeTime: c.closeTime, isActive: c.isActive });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setSaving(true);
    try {
      if (editingId) {
        await cinemaApi.update(editingId, { name: form.name, address: form.address, city: form.city, country: form.country, phoneNumber: form.phoneNumber || null, email: form.email || null, logoUrl: form.logoUrl || null, openTime: form.openTime, closeTime: form.closeTime, isActive: form.isActive } as UpdateCinemaDto);
        toast.success('Cinema updated.');
      } else {
        await cinemaApi.create({ name: form.name, address: form.address, city: form.city, country: form.country, phoneNumber: form.phoneNumber || null, email: form.email || null, logoUrl: form.logoUrl || null, openTime: form.openTime, closeTime: form.closeTime } as CreateCinemaDto);
        toast.success('Cinema created.');
      }
      setShowForm(false); setEditingId(null); await load();
    } catch (err) { toast.error(extractErrorMessage(err, 'Failed to save cinema')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try { await cinemaApi.delete(deleteId); toast.success('Cinema deleted.'); await load(); }
    catch (err) { toast.error(extractErrorMessage(err, 'Failed to delete cinema')); }
    finally { setDeleteId(null); }
  };

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ p: 1, borderRadius: 2, bgcolor: 'rgba(99,102,241,0.1)', color: '#6366f1' }}><Building2 size={24} /></Box>
            <Box>
              <Typography variant="h5" component="h1" fontWeight={700}>{t('cinemas.title')}</Typography>
              <Typography variant="body2" color="text.secondary">{cinemas.length} location{cinemas.length !== 1 ? 's' : ''}</Typography>
            </Box>
          </Box>
          <MuiButton variant="contained" startIcon={<Plus size={16} />} onClick={openCreate}>{t('cinemas.addCinema')}</MuiButton>
        </Box>

        {/* Inline form */}
        <AnimatePresence>
          {showForm && (
            <motion.div initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} style={{ overflow: 'hidden', marginBottom: 32 }}>
              <Paper variant="outlined" sx={{ borderRadius: 3, p: 3 }} component="form" onSubmit={handleSubmit}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                  <Typography variant="h6" component="h2" fontWeight={600}>{editingId ? t('cinemas.editCinema') : t('cinemas.addCinema')}</Typography>
                  <IconButton size="small" onClick={() => setShowForm(false)}><X size={18} /></IconButton>
                </Box>
                <Grid container spacing={2}>
                  <Grid size={12}>
                    <TextField label="Cinema Name *" name="name" value={form.name} onChange={e => set('name', e.target.value)} required fullWidth size="small" placeholder="Grand Cinema" />
                  </Grid>
                  <Grid size={12}>
                    <TextField label="Address *" name="address" value={form.address} onChange={e => set('address', e.target.value)} required fullWidth size="small" placeholder="123 Main Street" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="City *" name="city" value={form.city} onChange={e => set('city', e.target.value)} required fullWidth size="small" placeholder="London" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Country *" name="country" value={form.country} onChange={e => set('country', e.target.value)} required fullWidth size="small" placeholder="UK" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Phone" value={form.phoneNumber} onChange={e => set('phoneNumber', e.target.value)} fullWidth size="small" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Email" type="email" value={form.email} onChange={e => set('email', e.target.value)} fullWidth size="small" />
                  </Grid>
                  <Grid size={12}>
                    <TextField label="Logo URL" value={form.logoUrl} onChange={e => set('logoUrl', e.target.value)} fullWidth size="small" placeholder="https://..." />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Opening Time *" name="openTime" type="time" value={form.openTime} onChange={e => set('openTime', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Closing Time *" name="closeTime" type="time" value={form.closeTime} onChange={e => set('closeTime', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  {editingId && (
                    <Grid size={12}>
                      <FormControlLabel control={<Checkbox name="isActive" checked={form.isActive} onChange={e => set('isActive', e.target.checked)} size="small" />} label="Active" />
                    </Grid>
                  )}
                </Grid>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1.5, mt: 3 }}>
                  <MuiButton variant="outlined" onClick={() => setShowForm(false)}>{t('common.cancel')}</MuiButton>
                  <MuiButton type="submit" variant="contained" disabled={saving}>{saving ? t('common.saving') : editingId ? t('common.update') : t('common.create')}</MuiButton>
                </Box>
              </Paper>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Table */}
        {loading ? (
          <Stack spacing={1}>{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} height={52} sx={{ borderRadius: 2 }} />)}</Stack>
        ) : cinemas.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 10 }}>
            <Building2 size={48} color="rgba(128,128,128,0.3)" style={{ marginBottom: 16 }} />
            <Typography color="text.secondary">No cinemas yet. Add your first one!</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ '& th': { fontWeight: 600, bgcolor: 'action.hover' } }}>
                  <TableCell>{t('cinemas.columns.name')}</TableCell>
                  <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>{t('cinemas.columns.location')}</TableCell>
                  <TableCell sx={{ display: { xs: 'none', lg: 'table-cell' } }}>{t('cinemas.columns.contact')}</TableCell>
                  <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>{t('cinemas.columns.hours')}</TableCell>
                  <TableCell align="center">{t('cinemas.columns.halls')}</TableCell>
                  <TableCell>{t('cinemas.columns.status')}</TableCell>
                  <TableCell align="right">{t('cinemas.columns.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {cinemas.map(cinema => (
                  <TableRow key={cinema.id} hover sx={{ '& td': { py: 1.5 } }}>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        {cinema.logoUrl ? (
                          <Box component="img" src={cinema.logoUrl} alt="" sx={{ width: 32, height: 32, borderRadius: 1.5, objectFit: 'contain', border: '1px solid', borderColor: 'divider' }} />
                        ) : (
                          <Box sx={{ width: 32, height: 32, borderRadius: 1.5, bgcolor: 'rgba(99,102,241,0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6366f1' }}>
                            <Building2 size={16} />
                          </Box>
                        )}
                        <Typography fontWeight={500} variant="body2">{cinema.name}</Typography>
                      </Box>
                    </TableCell>
                    <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <MapPin size={11} />{cinema.city}, {cinema.country}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ display: { xs: 'none', lg: 'table-cell' } }}>
                      <Box>
                        {cinema.email && <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}><Mail size={11} />{cinema.email}</Typography>}
                        {cinema.phoneNumber && <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}><Phone size={11} />{cinema.phoneNumber}</Typography>}
                      </Box>
                    </TableCell>
                    <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <Clock size={11} />{cinema.openTime} – {cinema.closeTime}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">
                      <Typography variant="body2" fontFamily="monospace">{cinema.hallCount}</Typography>
                    </TableCell>
                    <TableCell>
                      <Badge variant={cinema.isActive ? 'success' : 'secondary'}>{cinema.isActive ? t('common.active') : t('common.inactive')}</Badge>
                    </TableCell>
                    <TableCell align="right">
                      <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5 }}>
                        <Tooltip title="Edit">
                          <IconButton aria-label="Edit" size="small" onClick={() => openEdit(cinema)}><Pencil size={14} /></IconButton>
                        </Tooltip>
                        <Tooltip title="Delete">
                          <IconButton aria-label="Delete" size="small" onClick={() => setDeleteId(cinema.id)} sx={{ color: 'error.main' }}><Trash2 size={14} /></IconButton>
                        </Tooltip>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Container>

      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }} title={t('cinemas.editCinema')} description={t('cinemas.confirmDelete')} confirmLabel={t('common.delete')} cancelLabel={t('common.cancel')} variant="destructive" onConfirm={handleDelete} />
    </Box>
  );
};
