import React, { useEffect, useState } from 'react';
import { showtimeApi } from '../../api/showtimeApi';
import { movieApi } from '../../api/movieApi';
import { hallApi } from '../../api/hallApi';
import { cinemaApi } from '../../api/cinemaApi';
import type { ShowtimeDto, MovieDto, CinemaHallDto, CinemaDto, CreateShowtimeDto, UpdateShowtimeDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

interface ShowtimeFormData {
  movieId: string;
  formCinemaId: string;
  cinemaHallId: string;
  startTime: string;
  basePrice: string;
  isActive: boolean;
}

const EMPTY_FORM: ShowtimeFormData = {
  movieId: '',
  formCinemaId: '',
  cinemaHallId: '',
  startTime: '',
  basePrice: '',
  isActive: true,
};

export const ShowtimesManagementPage: React.FC = () => {
  const [showtimes, setShowtimes] = useState<readonly ShowtimeDto[]>([]);
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [halls, setHalls] = useState<readonly CinemaHallDto[]>([]);
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [filterCinemaId, setFilterCinemaId] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<ShowtimeFormData>(EMPTY_FORM);
  const [filteredHalls, setFilteredHalls] = useState<readonly CinemaHallDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const loadData = async (cinemaId?: string) => {
    try {
      setLoading(true);
      const [showtimeData, movieData, hallData, cinemaData] = await Promise.all([
        showtimeApi.getAll(undefined, undefined, cinemaId || undefined),
        movieApi.getAll(true),
        hallApi.getAll(true),
        cinemaApi.getAll(true),
      ]);
      setShowtimes(showtimeData);
      setMovies(movieData);
      setHalls(hallData);
      setCinemas(cinemaData);
    } catch {
      setError('Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(filterCinemaId || undefined);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterCinemaId]);

  // Filter halls for form when cinema selection changes
  useEffect(() => {
    if (form.formCinemaId) {
      setFilteredHalls(halls.filter((h) => h.cinemaId === form.formCinemaId));
    } else {
      setFilteredHalls(halls);
    }
  }, [form.formCinemaId, halls]);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    if (type === 'checkbox') {
      const checked = (e.target as HTMLInputElement).checked;
      setForm((prev) => ({ ...prev, [name]: checked }));
      return;
    }
    setForm((prev) => {
      const updated = { ...prev, [name]: value };
      // Reset hall selection when cinema changes
      if (name === 'formCinemaId') {
        updated.cinemaHallId = '';
      }
      return updated;
    });
  };

  const handleCreate = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
    setError(null);
  };

  const handleEdit = (showtime: ShowtimeDto) => {
    setEditingId(showtime.id);
    setForm({
      movieId: showtime.movieId,
      formCinemaId: showtime.cinemaId,
      cinemaHallId: showtime.cinemaHallId,
      startTime: showtime.startTime.slice(0, 16),
      basePrice: showtime.basePrice.toString(),
      isActive: showtime.isActive,
    });
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this showtime?')) return;
    try {
      await showtimeApi.delete(id);
      await loadData(filterCinemaId || undefined);
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to delete showtime');
      setError(message);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (editingId) {
        const updateData: UpdateShowtimeDto = {
          startTime: new Date(form.startTime).toISOString(),
          basePrice: parseFloat(form.basePrice),
          isActive: form.isActive,
        };
        await showtimeApi.update(editingId, updateData);
      } else {
        const createData: CreateShowtimeDto = {
          movieId: form.movieId,
          cinemaHallId: form.cinemaHallId,
          startTime: new Date(form.startTime).toISOString(),
          basePrice: parseFloat(form.basePrice),
        };
        await showtimeApi.create(createData);
      }
      setEditingId(null);
      setShowForm(false);
      setForm(EMPTY_FORM);
      await loadData(filterCinemaId || undefined);
    } catch (err: unknown) {
      const message = extractErrorMessage(err, editingId ? 'Failed to update showtime' : 'Failed to create showtime');
      setError(message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div className="page-header">
          <h1>Showtimes Management</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + Add Showtime
          </button>
        </div>

        <div className="filters">
          <select
            value={filterCinemaId}
            onChange={(e) => setFilterCinemaId(e.target.value)}
            className="input"
            style={{ maxWidth: 260 }}
          >
            <option value="">All Cinemas</option>
            {cinemas.map((c) => (
              <option key={c.id} value={c.id}>{c.name} – {c.city}</option>
            ))}
          </select>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? 'Edit Showtime' : 'Schedule Showtime'}</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="movieId">Movie</label>
                  <select id="movieId" name="movieId" value={form.movieId} onChange={handleInputChange} className="input" required disabled={!!editingId}>
                    <option value="">Select a movie</option>
                    {movies.map((movie) => (
                      <option key={movie.id} value={movie.id}>{movie.title}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label htmlFor="formCinemaId">Cinema</label>
                  <select id="formCinemaId" name="formCinemaId" value={form.formCinemaId} onChange={handleInputChange} className="input" required disabled={!!editingId}>
                    <option value="">Select a cinema</option>
                    {cinemas.map((c) => (
                      <option key={c.id} value={c.id}>{c.name} – {c.city}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label htmlFor="cinemaHallId">Cinema Hall</label>
                  <select id="cinemaHallId" name="cinemaHallId" value={form.cinemaHallId} onChange={handleInputChange} className="input" required disabled={!form.formCinemaId || !!editingId}>
                    <option value="">Select a hall</option>
                    {filteredHalls.map((hall) => (
                      <option key={hall.id} value={hall.id}>{hall.name} ({hall.totalSeats} seats)</option>
                    ))}
                  </select>
                  {form.formCinemaId && filteredHalls.length === 0 && (
                    <p className="form-help" style={{ color: '#e53e3e' }}>No active halls for this cinema.</p>
                  )}
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="startTime">Start Time</label>
                    <input id="startTime" name="startTime" type="datetime-local" value={form.startTime} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="basePrice">Base Price ($)</label>
                    <input id="basePrice" name="basePrice" type="number" step="0.01" min="0" value={form.basePrice} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                {editingId && (
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        name="isActive"
                        checked={form.isActive}
                        onChange={handleInputChange}
                      />
                      Active
                    </label>
                  </div>
                )}
                <div className="form-actions">
                  <button type="button" onClick={() => setShowForm(false)} className="btn btn-outline">Cancel</button>
                  <button type="submit" className="btn btn-primary" disabled={saving}>
                    {saving ? 'Saving...' : editingId ? 'Update' : 'Create'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        <div className="data-table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Movie</th>
                <th>Cinema</th>
                <th>Hall</th>
                <th>Start Time</th>
                <th>Base Price</th>
                <th>Available Seats</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {showtimes.map((showtime) => (
                <tr key={showtime.id}>
                  <td>{showtime.movieTitle}</td>
                  <td>{showtime.cinemaName}</td>
                  <td>{showtime.hallName}</td>
                  <td>{formatDateTime(showtime.startTime)}</td>
                  <td>{formatCurrency(showtime.basePrice)}</td>
                  <td>{showtime.availableSeats}</td>
                  <td>
                    <span className={`status-badge status-${showtime.isActive ? 'confirmed' : 'cancelled'}`}>
                      {showtime.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div className="table-actions">
                      <button onClick={() => handleEdit(showtime)} className="btn btn-sm btn-outline">Edit</button>
                      <button onClick={() => handleDelete(showtime.id)} className="btn btn-sm btn-danger">Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
