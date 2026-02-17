import React, { useEffect, useState } from 'react';
import { showtimeApi } from '../../api/showtimeApi';
import { movieApi } from '../../api/movieApi';
import { hallApi } from '../../api/hallApi';
import type { ShowtimeDto, MovieDto, CinemaHallDto, CreateShowtimeDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

interface ShowtimeFormData {
  movieId: string;
  cinemaHallId: string;
  startTime: string;
  basePrice: string;
}

const EMPTY_FORM: ShowtimeFormData = {
  movieId: '',
  cinemaHallId: '',
  startTime: '',
  basePrice: '',
};

export const ShowtimesManagementPage: React.FC = () => {
  const [showtimes, setShowtimes] = useState<readonly ShowtimeDto[]>([]);
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [halls, setHalls] = useState<readonly CinemaHallDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<ShowtimeFormData>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadData = async () => {
    try {
      setLoading(true);
      const [showtimeData, movieData, hallData] = await Promise.all([
        showtimeApi.getAll(),
        movieApi.getAll(true),
        hallApi.getAll(true),
      ]);
      setShowtimes(showtimeData);
      setMovies(movieData);
      setHalls(hallData);
    } catch {
      setError('Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreate = () => {
    setForm(EMPTY_FORM);
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this showtime?')) return;
    try {
      await showtimeApi.delete(id);
      await loadData();
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
      const createData: CreateShowtimeDto = {
        movieId: form.movieId,
        cinemaHallId: form.cinemaHallId,
        startTime: new Date(form.startTime).toISOString(),
        basePrice: parseFloat(form.basePrice),
      };
      await showtimeApi.create(createData);
      setShowForm(false);
      setForm(EMPTY_FORM);
      await loadData();
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to create showtime');
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

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2>Schedule Showtime</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="movieId">Movie</label>
                  <select id="movieId" name="movieId" value={form.movieId} onChange={handleInputChange} className="input" required>
                    <option value="">Select a movie</option>
                    {movies.map((movie) => (
                      <option key={movie.id} value={movie.id}>{movie.title}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label htmlFor="cinemaHallId">Cinema Hall</label>
                  <select id="cinemaHallId" name="cinemaHallId" value={form.cinemaHallId} onChange={handleInputChange} className="input" required>
                    <option value="">Select a hall</option>
                    {halls.map((hall) => (
                      <option key={hall.id} value={hall.id}>{hall.name} ({hall.totalSeats} seats)</option>
                    ))}
                  </select>
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
                <div className="form-actions">
                  <button type="button" onClick={() => setShowForm(false)} className="btn btn-outline">Cancel</button>
                  <button type="submit" className="btn btn-primary" disabled={saving}>
                    {saving ? 'Saving...' : 'Create'}
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
