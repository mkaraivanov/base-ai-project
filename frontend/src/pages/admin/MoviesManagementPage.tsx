import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { movieApi } from '../../api/movieApi';
import type { MovieDto, CreateMovieDto, UpdateMovieDto } from '../../types';
import { formatDate, formatDuration } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

interface MovieFormData {
  title: string;
  description: string;
  genre: string;
  durationMinutes: string;
  rating: string;
  posterUrl: string;
  releaseDate: string;
  isActive: boolean;
}

const EMPTY_FORM: MovieFormData = {
  title: '',
  description: '',
  genre: '',
  durationMinutes: '',
  rating: '',
  posterUrl: '',
  releaseDate: '',
  isActive: true,
};

export const MoviesManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<MovieFormData>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadMovies = async () => {
    try {
      setLoading(true);
      const data = await movieApi.getAll(false);
      setMovies(data);
    } catch {
      setError('Failed to load movies');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMovies();
  }, []);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    if (type === 'checkbox') {
      const checked = (e.target as HTMLInputElement).checked;
      setForm((prev) => ({ ...prev, [name]: checked }));
    } else {
      setForm((prev) => ({ ...prev, [name]: value }));
    }
  };

  const handleCreate = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
    setError(null);
  };

  const handleEdit = (movie: MovieDto) => {
    setEditingId(movie.id);
    setForm({
      title: movie.title,
      description: movie.description,
      genre: movie.genre,
      durationMinutes: movie.durationMinutes.toString(),
      rating: movie.rating,
      posterUrl: movie.posterUrl,
      releaseDate: movie.releaseDate,
      isActive: movie.isActive,
    });
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('movies.confirmDelete'))) return;
    try {
      await movieApi.delete(id);
      await loadMovies();
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to delete movie');
      setError(message);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (editingId) {
        const updateData: UpdateMovieDto = {
          title: form.title,
          description: form.description,
          genre: form.genre,
          durationMinutes: parseInt(form.durationMinutes, 10),
          rating: form.rating,
          posterUrl: form.posterUrl,
          releaseDate: form.releaseDate,
          isActive: form.isActive,
        };
        await movieApi.update(editingId, updateData);
      } else {
        const createData: CreateMovieDto = {
          title: form.title,
          description: form.description,
          genre: form.genre,
          durationMinutes: parseInt(form.durationMinutes, 10),
          rating: form.rating,
          posterUrl: form.posterUrl,
          releaseDate: form.releaseDate,
        };
        await movieApi.create(createData);
      }
      setShowForm(false);
      setForm(EMPTY_FORM);
      setEditingId(null);
      await loadMovies();
    } catch (err: unknown) {
      const message = extractErrorMessage(
        err,
        editingId ? 'Failed to update movie' : 'Failed to create movie'
      );
      setError(message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="page"><div className="loading">{t('common.loading')}</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div className="page-header">
          <h1>{t('movies.title')}</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + {t('movies.addMovie')}
          </button>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? t('movies.editMovie') : t('movies.addMovie')}</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="title">{t('movies.form.title')}</label>
                  <input id="title" name="title" value={form.title} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-group">
                  <label htmlFor="description">{t('movies.form.description')}</label>
                  <textarea id="description" name="description" value={form.description} onChange={handleInputChange} className="input" rows={3} required />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="genre">{t('movies.form.genre')}</label>
                    <input id="genre" name="genre" value={form.genre} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="rating">{t('movies.form.rating')}</label>
                    <input id="rating" name="rating" value={form.rating} onChange={handleInputChange} className="input" placeholder={t('movies.form.ratingPlaceholder')} required />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="durationMinutes">{t('movies.form.duration')}</label>
                    <input id="durationMinutes" name="durationMinutes" type="number" value={form.durationMinutes} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="releaseDate">{t('movies.form.releaseDate')}</label>
                    <input id="releaseDate" name="releaseDate" type="date" value={form.releaseDate} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                <div className="form-group">
                  <label htmlFor="posterUrl">{t('movies.form.posterUrl')}</label>
                  <input id="posterUrl" name="posterUrl" value={form.posterUrl} onChange={handleInputChange} className="input" />
                </div>
                {editingId && (
                  <div className="form-group form-check">
                    <label>
                      <input type="checkbox" name="isActive" checked={form.isActive} onChange={handleInputChange} />
                      {t('movies.form.activeLabel')}
                    </label>
                  </div>
                )}
                <div className="form-actions">
                  <button type="button" onClick={() => setShowForm(false)} className="btn btn-outline">{t('common.cancel')}</button>
                  <button type="submit" className="btn btn-primary" disabled={saving}>
                    {saving ? t('common.saving') : editingId ? t('common.update') : t('common.create')}
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
                <th>{t('movies.columns.title')}</th>
                <th>{t('movies.columns.genre')}</th>
                <th>{t('movies.columns.duration')}</th>
                <th>{t('movies.columns.rating')}</th>
                <th>{t('movies.columns.releaseDate')}</th>
                <th>{t('movies.columns.status')}</th>
                <th>{t('movies.columns.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {movies.map((movie) => (
                <tr key={movie.id}>
                  <td>{movie.title}</td>
                  <td>{movie.genre}</td>
                  <td>{formatDuration(movie.durationMinutes)}</td>
                  <td>{movie.rating}</td>
                  <td>{formatDate(movie.releaseDate)}</td>
                  <td>
                    <span className={`status-badge status-${movie.isActive ? 'confirmed' : 'cancelled'}`}>
                      {movie.isActive ? t('common.active') : t('common.inactive')}
                    </span>
                  </td>
                  <td>
                    <div className="table-actions">
                      <button onClick={() => handleEdit(movie)} className="btn btn-sm btn-outline">{t('common.edit')}</button>
                      <button onClick={() => handleDelete(movie.id)} className="btn btn-sm btn-danger">{t('common.delete')}</button>
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
