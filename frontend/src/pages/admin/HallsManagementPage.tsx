import React, { useCallback, useEffect, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Pencil, Trash2, X, LayoutGrid, Users } from 'lucide-react';
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
import Alert from '@mui/material/Alert';
import { hallApi } from '../../api/hallApi';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaHallDto, CreateCinemaHallDto, UpdateCinemaHallDto, SeatLayout, SeatDefinition, CinemaDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';
import { Badge } from '../../components/ui/badge';
import { AlertDialog } from '../../components/ui/alert-dialog';

interface HallFormData { cinemaId: string; name: string; rows: string; seatsPerRow: string; isActive: boolean; seatLayout?: SeatLayout; }
const EMPTY: HallFormData = { cinemaId: '', name: '', rows: '10', seatsPerRow: '15', isActive: true };

function generateSeatLayout(rows: number, seatsPerRow: number): SeatLayout {
  const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  const premiumStart = Math.max(0, rows - 2);
  const seats: SeatDefinition[] = [];
  for (let r = 0; r < rows; r++) {
    for (let s = 0; s < seatsPerRow; s++) {
      const seatType = r >= premiumStart ? 'Premium' : 'Regular';
      const priceMultiplier = seatType === 'Premium' ? 1.5 : 1.0;
      seats.push({ seatNumber: `${letters[r]}${s + 1}`, row: r, column: s, seatType, priceMultiplier, isAvailable: true });
    }
  }
  return { rows, seatsPerRow, seats };
}

export const HallsManagementPage: React.FC = () => {
  const [halls, setHalls] = useState<readonly CinemaHallDto[]>([]);
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<HallFormData>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [filterCinemaId, setFilterCinemaId] = useState('');

  const loadHalls = useCallback(async () => {
    try { setLoading(true); setHalls(await hallApi.getAll(false, filterCinemaId || undefined)); }
    catch { toast.error('Failed to load halls'); }
    finally { setLoading(false); }
  }, [filterCinemaId]);

  useEffect(() => { loadHalls(); }, [loadHalls]);
  useEffect(() => { cinemaApi.getAll(true).then(setCinemas).catch(() => {}); }, []);

  const set = (name: keyof HallFormData, v: string | boolean) => setForm(p => ({ ...p, [name]: v }));

  const openCreate = () => { setEditingId(null); setForm(EMPTY); setShowForm(true); };
  const openEdit = (h: CinemaHallDto) => {
    setEditingId(h.id);
    setForm({ cinemaId: h.cinemaId, name: h.name, rows: h.seatLayout.rows.toString(), seatsPerRow: h.seatLayout.seatsPerRow.toString(), isActive: h.isActive, seatLayout: h.seatLayout });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setSaving(true);
    try {
      const rows = parseInt(form.rows, 10);
      const seatsPerRow = parseInt(form.seatsPerRow, 10);
      if (editingId) {
        const existingSeatLayout = form.seatLayout ?? generateSeatLayout(rows, seatsPerRow);
        await hallApi.update(editingId, { name: form.name, seatLayout: existingSeatLayout, isActive: form.isActive });
        toast.success('Hall updated.');
      } else {
        const layout = generateSeatLayout(rows, seatsPerRow);
        await hallApi.create({ cinemaId: form.cinemaId, name: form.name, seatLayout: layout });
        toast.success('Hall created.');
      }
      setShowForm(false); setEditingId(null); await loadHalls();
    } catch (err) { toast.error(extractErrorMessage(err, 'Failed to save hall')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try { await hallApi.delete(deleteId); toast.success('Hall deleted.'); await loadHalls(); }
    catch (err) { toast.error(extractErrorMessage(err, 'Failed to delete hall')); }
    finally { setDeleteId(null); }
  };

  const totalPrev = parseInt(form.rows || '0', 10) * parseInt(form.seatsPerRow || '0', 10);
  const cinemaNamed = (id: string) => cinemas.find(c => c.id === id)?.name ?? id;

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ p: 1, borderRadius: 2, bgcolor: 'rgba(139,92,246,0.1)', color: '#8b5cf6' }}><LayoutGrid size={24} /></Box>
            <Box>
              <Typography variant="h5" component="h1" fontWeight={700}>Cinema Halls Management</Typography>
              <Typography variant="body2" color="text.secondary">{halls.length} hall{halls.length !== 1 ? 's' : ''}</Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Filter by Cinema</InputLabel>
              <Select value={filterCinemaId} label="Filter by Cinema" onChange={(e: SelectChangeEvent) => setFilterCinemaId(e.target.value)}>
                <MenuItem value="">All Cinemas</MenuItem>
                {cinemas.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
              </Select>
            </FormControl>
            <MuiButton variant="contained" startIcon={<Plus size={16} />} onClick={openCreate}>Add Hall</MuiButton>
          </Box>
        </Box>

        <AnimatePresence>
          {showForm && (
            <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} style={{ marginBottom: 32 }}>
              <Paper className="modal" variant="outlined" sx={{ borderRadius: 3, p: 3 }} component="form" onSubmit={handleSubmit}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography variant="h6" component="h2" fontWeight={600}>{editingId ? 'Edit Hall' : 'Add Hall'}</Typography>
                  <IconButton size="small" onClick={() => setShowForm(false)}><X size={18} /></IconButton>
                </Box>
                <Grid container spacing={2}>
                  {!editingId && (
                    <Grid size={12}>
                      <FormControl size="small" fullWidth required>
                        <InputLabel>Cinema *</InputLabel>
                        <Select value={form.cinemaId} label="Cinema *" onChange={(e: SelectChangeEvent) => set('cinemaId', e.target.value)} inputProps={{ name: 'cinemaId', required: true }}>
                          {cinemas.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
                        </Select>
                      </FormControl>
                    </Grid>
                  )}
                  <Grid size={12}>
                    <TextField label="Hall Name *" name="name" id="name" value={form.name} onChange={e => set('name', e.target.value)} required fullWidth size="small" />
                  </Grid>
                  {!editingId && (
                    <>
                      <Grid size={6}>
                        <TextField label="Rows (1–26) *" name="rows" type="number" slotProps={{ htmlInput: { min: 1, max: 26 } }} value={form.rows} onChange={e => set('rows', e.target.value)} required fullWidth size="small" />
                      </Grid>
                      <Grid size={6}>
                        <TextField label="Seats per Row (1–30) *" name="seatsPerRow" type="number" slotProps={{ htmlInput: { min: 1, max: 30 } }} value={form.seatsPerRow} onChange={e => set('seatsPerRow', e.target.value)} required fullWidth size="small" />
                      </Grid>
                      {totalPrev > 0 && (
                        <Grid size={12}>
                          <Alert severity="info" sx={{ borderRadius: 2 }}>
                            Total: <strong>{totalPrev} seats</strong> — last 2 rows will be <strong>Premium (×1.5)</strong>
                          </Alert>
                        </Grid>
                      )}
                    </>
                  )}
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
          <Stack spacing={1}>{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} height={48} sx={{ borderRadius: 2 }} />)}</Stack>
        ) : (
          <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ '& th': { fontWeight: 600, bgcolor: 'action.hover' } }}>
                  <TableCell>Hall</TableCell>
                  <TableCell>Cinema</TableCell>
                  <TableCell>Layout</TableCell>
                  <TableCell>Seats</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {halls.map(h => (
                  <TableRow key={h.id} hover>
                    <TableCell><Typography variant="body2" fontWeight={500}>{h.name}</Typography></TableCell>
                    <TableCell><Typography variant="body2" color="text.secondary">{cinemaNamed(h.cinemaId)}</Typography></TableCell>
                    <TableCell><Typography variant="body2" fontFamily="monospace">{h.rows} × {h.seatsPerRow}</Typography></TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}><Users size={13} /><Typography variant="body2">{h.totalSeats}</Typography></Box>
                    </TableCell>
                    <TableCell><Badge variant={h.isActive ? 'success' : 'secondary'}>{h.isActive ? 'Active' : 'Inactive'}</Badge></TableCell>
                    <TableCell align="right">
                      <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                        <Tooltip title="Edit"><IconButton aria-label="Edit" size="small" onClick={() => openEdit(h)}><Pencil size={13} /></IconButton></Tooltip>
                        <Tooltip title="Delete"><IconButton aria-label="Delete" size="small" onClick={() => setDeleteId(h.id)} sx={{ color: 'error.main' }}><Trash2 size={13} /></IconButton></Tooltip>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Container>

      <AlertDialog open={!!deleteId} onOpenChange={o => { if (!o) setDeleteId(null); }} title="Delete Hall?" description="This will permanently remove the hall and all its seats. This action cannot be undone." confirmLabel="Delete" variant="destructive" onConfirm={handleDelete} />
    </Box>
  );
};
