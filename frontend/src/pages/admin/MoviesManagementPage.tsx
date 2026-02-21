import React, { useEffect, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Pencil, Trash2, X, Film } from 'lucide-react';
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
import { movieApi } from '../../api/movieApi';
import type { MovieDto, CreateMovieDto, UpdateMovieDto } from '../../types';
import { formatDate, formatDuration } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';
import { Badge } from '../../components/ui/badge';
import { AlertDialog } from '../../components/ui/alert-dialog';

interface MovieFormData {
  title: string; description: string; genre: string;
  durationMinutes: string; rating: string; posterUrl: string;
  releaseDate: string; isActive: boolean;
}

const EMPTY: MovieFormData = { title: '', description: '', genre: '', durationMinutes: '', rating: '', posterUrl: '', releaseDate: '', isActive: true };

export const MoviesManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<MovieFormData>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const load = async () => {
    try { setLoading(true); setMovies(await movieApi.getAll(false)); }
    catch { toast.error('Failed to load movies'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const set = (name: keyof MovieFormData, value: string | boolean) =>
    setForm(prev => ({ ...prev, [name]: value }));

  const openCreate = () => { setEditingId(null); setForm(EMPTY); setShowForm(true); };
  const openEdit = (m: MovieDto) => {
    setEditingId(m.id);
    setForm({ title: m.title, description: m.description, genre: m.genre, durationMinutes: m.durationMinutes.toString(), rating: m.rating, posterUrl: m.posterUrl, releaseDate: m.releaseDate, isActive: m.isActive });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setSaving(true);
    try {
      if (editingId) {
        await movieApi.update(editingId, { title: form.title, description: form.description, genre: form.genre, durationMinutes: parseInt(form.durationMinutes, 10), rating: form.rating, posterUrl: form.posterUrl, releaseDate: form.releaseDate, isActive: form.isActive } as UpdateMovieDto);
        toast.success('Movie updated.');
      } else {
        await movieApi.create({ title: form.title, description: form.description, genre: form.genre, durationMinutes: parseInt(form.durationMinutes, 10), rating: form.rating, posterUrl: form.posterUrl, releaseDate: form.releaseDate } as CreateMovieDto);
        toast.success('Movie created.');
      }
      setShowForm(false); setEditingId(null); await load();
    } catch (err) { toast.error(extractErrorMessage(err, 'Failed to save movie')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try { await movieApi.delete(deleteId); toast.success('Movie deleted.'); await load(); }
    catch (err) { toast.error(extractErrorMessage(err, 'Failed to delete movie')); }
    finally { setDeleteId(null); }
  };

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ p: 1, borderRadius: 2, bgcolor: 'rgba(168,85,247,0.1)', color: '#a855f7' }}><Film size={24} /></Box>
            <Box>
              <Typography variant="h5" component="h1" fontWeight={700}>{t('movies.title')}</Typography>
              <Typography variant="body2" color="text.secondary">{movies.length} title{movies.length !== 1 ? 's' : ''}</Typography>
            </Box>
          </Box>
          <MuiButton variant="contained" startIcon={<Plus size={16} />} onClick={openCreate}>{t('movies.addMovie')}</MuiButton>
        </Box>

        <AnimatePresence>
          {showForm && (
            <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} style={{ marginBottom: 32 }}>
              <Paper variant="outlined" sx={{ borderRadius: 3, p: 3 }} component="form" onSubmit={handleSubmit}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography variant="h6" component="h2" fontWeight={600}>{editingId ? t('movies.editMovie') : t('movies.addMovie')}</Typography>
                  <IconButton size="small" onClick={() => setShowForm(false)}><X size={18} /></IconButton>
                </Box>
                <Grid container spacing={2}>
                  <Grid size={12}>
                    <TextField label="Title *" name="title" value={form.title} onChange={e => set('title', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  <Grid size={12}>
                    <TextField label="Description *" name="description" value={form.description} onChange={e => set('description', e.target.value)} required fullWidth multiline rows={3} size="small" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Genre *" name="genre" value={form.genre} onChange={e => set('genre', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Rating *" name="rating" value={form.rating} onChange={e => set('rating', e.target.value)} required fullWidth size="small" placeholder="PG-13" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Duration (min) *" name="durationMinutes" type="number" slotProps={{ htmlInput: { min: 1 } }} value={form.durationMinutes} onChange={e => set('durationMinutes', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  <Grid size={6}>
                    <TextField label="Release Date *" name="releaseDate" type="date" value={form.releaseDate} onChange={e => set('releaseDate', e.target.value)} required fullWidth size="small" slotProps={{ inputLabel: { shrink: true } }} />
                  </Grid>
                  <Grid size={12}>
                    <TextField label="Poster URL" name="posterUrl" value={form.posterUrl} onChange={e => set('posterUrl', e.target.value)} fullWidth size="small" />
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
                  {[t('movies.columns.title'), t('movies.columns.genre'), t('movies.columns.duration'), t('movies.columns.rating'), t('movies.columns.releaseDate'), t('movies.columns.status'), ''].map(h => (
                    <TableCell key={h}><Typography variant="caption" fontWeight={600} textTransform="uppercase" letterSpacing={0.5}>{h}</Typography></TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {movies.map(movie => (
                  <TableRow key={movie.id} hover>
                    <TableCell><Typography variant="body2" fontWeight={500}>{movie.title}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{movie.genre}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{formatDuration(movie.durationMinutes)}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{movie.rating}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{formatDate(movie.releaseDate)}</Typography></TableCell>
                    <TableCell><Badge variant={movie.isActive ? 'success' : 'secondary'}>{movie.isActive ? t('common.active') : t('common.inactive')}</Badge></TableCell>
                    <TableCell align="right">
                      <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                        <Tooltip title="Edit"><IconButton aria-label="Edit" size="small" onClick={() => openEdit(movie)}><Pencil size={13} /></IconButton></Tooltip>
                        <Tooltip title="Delete"><IconButton aria-label="Delete" size="small" onClick={() => setDeleteId(movie.id)} sx={{ color: 'error.main' }}><Trash2 size={13} /></IconButton></Tooltip>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Container>

      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }} title={t('movies.deleteMovie')} description={t('movies.confirmDelete')} confirmLabel={t('common.delete')} variant="destructive" onConfirm={handleDelete} />
    </Box>
  );
};
