import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Pencil, Trash2, X, Calendar, Film, DollarSign } from 'lucide-react';
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
import Select, { SelectChangeEvent } from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import InputLabel from '@mui/material/InputLabel';
import FormControl from '@mui/material/FormControl';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import FormControlLabel from '@mui/material/FormControlLabel';
import Checkbox from '@mui/material/Checkbox';
import { showtimeApi } from '../../api/showtimeApi';
import { movieApi } from '../../api/movieApi';
import { hallApi } from '../../api/hallApi';
import { cinemaApi } from '../../api/cinemaApi';
import type { ShowtimeDto, CreateShowtimeDto, UpdateShowtimeDto, MovieDto, CinemaHallDto, CinemaDto } from '../../types';
import { formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';
import { Badge } from '../../components/ui/badge';
import { AlertDialog } from '../../components/ui/alert-dialog';

interface ShowtimeFormData {
  movieId: string; formCinemaId: string; cinemaHallId: string;
  startTime: string; basePrice: string; isActive: boolean;
}
const EMPTY: ShowtimeFormData = { movieId: '', formCinemaId: '', cinemaHallId: '', startTime: '', basePrice: '', isActive: true };

export const ShowtimesManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [showtimes, setShowtimes] = useState<readonly ShowtimeDto[]>([]);
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [halls, setHalls] = useState<readonly CinemaHallDto[]>([]);
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ShowtimeFormData>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [filterCinemaId, setFilterCinemaId] = useState('');

  const loadShowtimes = useCallback(async () => {
    try { setLoading(true); setShowtimes(await showtimeApi.getAll(filterCinemaId || undefined)); }
    catch { toast.error('Failed to load showtimes'); }
    finally { setLoading(false); }
  }, [filterCinemaId]);

  useEffect(() => { loadShowtimes(); }, [loadShowtimes]);
  useEffect(() => {
    Promise.all([movieApi.getAll(true), hallApi.getAll(true), cinemaApi.getAll(true)])
      .then(([m, h, c]) => { setMovies(m); setHalls(h); setCinemas(c); })
      .catch(() => {});
  }, []);

  const filteredHalls = useMemo(() => halls.filter(h => h.cinemaId === form.formCinemaId), [halls, form.formCinemaId]);

  const set = (name: keyof ShowtimeFormData, v: string | boolean) =>
    setForm(p => name === 'formCinemaId' ? { ...p, [name]: v as string, cinemaHallId: '' } : { ...p, [name]: v });

  const openCreate = () => { setEditingId(null); setForm(EMPTY); setShowForm(true); };
  const openEdit = (st: ShowtimeDto) => {
    setEditingId(st.id);
    const hall = halls.find(h => h.id === st.cinemaHallId);
    setForm({ movieId: st.movieId, formCinemaId: hall?.cinemaId ?? '', cinemaHallId: st.cinemaHallId, startTime: st.startTime.slice(0, 16), basePrice: st.basePrice.toString(), isActive: st.isActive });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setSaving(true);
    try {
      if (editingId) {
        await showtimeApi.update(editingId, { basePrice: parseFloat(form.basePrice), isActive: form.isActive } as UpdateShowtimeDto);
        toast.success('Showtime updated.');
      } else {
        await showtimeApi.create({ movieId: form.movieId, cinemaHallId: form.cinemaHallId, startTime: new Date(form.startTime).toISOString(), basePrice: parseFloat(form.basePrice) } as CreateShowtimeDto);
        toast.success('Showtime created.');
      }
      setShowForm(false); setEditingId(null); await loadShowtimes();
    } catch (err) { toast.error(extractErrorMessage(err, 'Failed to save showtime')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try { await showtimeApi.delete(deleteId); toast.success('Showtime deleted.'); await loadShowtimes(); }
    catch (err) { toast.error(extractErrorMessage(err, 'Failed to delete showtime')); }
    finally { setDeleteId(null); }
  };

  const movieTitle = (id: string) => movies.find(m => m.id === id)?.title ?? id;
  const hallName = (id: string) => halls.find(h => h.id === id)?.name ?? id;
  const cinemaNamed = (id: string) => cinemas.find(c => c.id === id)?.name ?? '';

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ p: 1, borderRadius: 2, bgcolor: 'rgba(245,158,11,0.1)', color: '#f59e0b' }}><Calendar size={24} /></Box>
            <Box>
              <Typography variant="h5" component="h1" fontWeight={700}>{t('showtimes.title')}</Typography>
              <Typography variant="body2" color="text.secondary">{showtimes.length} showtime{showtimes.length !== 1 ? 's' : ''}</Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>{t('common.allCinemas')}</InputLabel>
              <Select value={filterCinemaId} label={t('common.allCinemas')} onChange={(e: SelectChangeEvent) => setFilterCinemaId(e.target.value)}>
                <MenuItem value="">{t('common.allCinemas')}</MenuItem>
                {cinemas.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
              </Select>
            </FormControl>
            <MuiButton variant="contained" startIcon={<Plus size={16} />} onClick={openCreate}>{t('showtimes.addShowtime')}</MuiButton>
          </Box>
        </Box>

        <AnimatePresence>
          {showForm && (
            <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} style={{ marginBottom: 32 }}>
              <Paper variant="outlined" sx={{ borderRadius: 3, p: 3 }} component="form" onSubmit={handleSubmit}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography variant="h6" component="h2" fontWeight={600}>{editingId ? t('showtimes.editShowtime') : t('showtimes.scheduleShowtime')}</Typography>
                  <IconButton size="small" onClick={() => setShowForm(false)}><X size={18} /></IconButton>
                </Box>
                <Grid container spacing={2}>
                  <Grid size={12}>
                    <FormControl size="small" fullWidth required disabled={!!editingId}>
                      <InputLabel>Movie *</InputLabel>
                      <Select inputProps={{ name: 'movieId', required: true }} value={form.movieId} label="Movie *" onChange={(e: SelectChangeEvent) => set('movieId', e.target.value)}>
                        {movies.map(m => <MenuItem key={m.id} value={m.id}>{m.title}</MenuItem>)}
                      </Select>
                    </FormControl>
                  </Grid>
                  <Grid size={6}>
                    <FormControl size="small" fullWidth required disabled={!!editingId}>
                      <InputLabel>Cinema *</InputLabel>
                      <Select value={form.formCinemaId} label="Cinema *" onChange={(e: SelectChangeEvent) => set('formCinemaId', e.target.value)} inputProps={{ name: 'formCinemaId' }}>
                        {cinemas.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
                      </Select>
                    </FormControl>
                  </Grid>
                  <Grid size={6}>
                    <FormControl size="small" fullWidth required disabled={!!editingId || !form.formCinemaId}>
                      <InputLabel>Hall *</InputLabel>
                      <Select inputProps={{ name: 'cinemaHallId', required: true }} value={form.cinemaHallId} label="Hall *" onChange={(e: SelectChangeEvent) => set('cinemaHallId', e.target.value)}>
                        {filteredHalls.map(h => <MenuItem key={h.id} value={h.id}>{h.name}</MenuItem>)}
                      </Select>
                    </FormControl>
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Start Time *" name="startTime" type="datetime-local" value={form.startTime} onChange={e => set('startTime', e.target.value)} required fullWidth size="small" slotProps={{ inputLabel: { shrink: true } }} disabled={!!editingId} />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Base Price *" name="basePrice" type="number" slotProps={{ htmlInput: { min: 0, step: 0.01 } }} value={form.basePrice} onChange={e => set('basePrice', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  {editingId && (
                    <Grid size={12}>
                      <FormControlLabel control={<Checkbox checked={form.isActive} onChange={e => set('isActive', e.target.checked)} size="small" />} label="Active" />
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

        {loading ? (
          <Stack spacing={1}>{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} height={48} sx={{ borderRadius: 2 }} />)}</Stack>
        ) : (
          <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ '& th': { fontWeight: 600, bgcolor: 'action.hover' } }}>
                  <TableCell>{t('showtimes.columns.movie')}</TableCell>
                  <TableCell>{t('showtimes.columns.cinema')} / {t('showtimes.columns.hall')}</TableCell>
                  <TableCell>{t('showtimes.columns.startTime')}</TableCell>
                  <TableCell>{t('showtimes.columns.price')}</TableCell>
                  <TableCell>{t('showtimes.columns.availableSeats')}</TableCell>
                  <TableCell>{t('showtimes.columns.status')}</TableCell>
                  <TableCell align="right">{t('showtimes.columns.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {showtimes.map(st => {
                  const hall = halls.find(h => h.id === st.cinemaHallId);
                  return (
                    <TableRow key={st.id} hover>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Film size={13} style={{ opacity: 0.5 }} />
                          <Typography variant="body2" fontWeight={500}>{movieTitle(st.movieId)}</Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {hall ? `${cinemaNamed(hall.cinemaId)} / ${hallName(st.cinemaHallId)}` : hallName(st.cinemaHallId)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(st.startTime).toLocaleString()}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <DollarSign size={12} style={{ opacity: 0.5 }} />
                          <Typography variant="body2">{formatCurrency(st.basePrice)}</Typography>
                        </Box>
                      </TableCell>
                      <TableCell><Typography variant="body2">{st.availableSeats}</Typography></TableCell>
                      <TableCell><Badge variant={st.isActive ? 'success' : 'secondary'}>{st.isActive ? t('common.active') : t('common.inactive')}</Badge></TableCell>
                      <TableCell align="right">
                        <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                          <Tooltip title="Edit"><IconButton aria-label="Edit" size="small" onClick={() => openEdit(st)}><Pencil size={13} /></IconButton></Tooltip>
                          <Tooltip title="Delete"><IconButton aria-label="Delete" size="small" onClick={() => setDeleteId(st.id)} sx={{ color: 'error.main' }}><Trash2 size={13} /></IconButton></Tooltip>
                        </Box>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Container>

      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }} title={t('showtimes.confirmDelete')} description={t('showtimes.confirmDelete')} confirmLabel={t('common.delete')} variant="destructive" onConfirm={handleDelete} />
    </Box>
  );
};
