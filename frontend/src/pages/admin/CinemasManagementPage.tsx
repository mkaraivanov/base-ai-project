import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto, CreateCinemaDto, UpdateCinemaDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';

interface CinemaFormData {
  name: string;
  address: string;
  city: string;
  country: string;
  phoneNumber: string;
  email: string;
  logoUrl: string;
  openTime: string;
  closeTime: string;
  isActive: boolean;
}

const EMPTY_FORM: CinemaFormData = {
  name: '',
  address: '',
  city: '',
  country: '',
  phoneNumber: '',
  email: '',
  logoUrl: '',
  openTime: '09:00',
  closeTime: '23:00',
  isActive: true,
};

export const CinemasManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CinemaFormData>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadCinemas = async () => {
    try {
      setLoading(true);
      const data = await cinemaApi.getAll(false);
      setCinemas(data);
    } catch {
      setError('Failed to load cinemas');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCinemas();
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleCreate = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
    setError(null);
  };

  const handleEdit = (cinema: CinemaDto) => {
    setEditingId(cinema.id);
    setForm({
      name: cinema.name,
      address: cinema.address,
      city: cinema.city,
      country: cinema.country,
      phoneNumber: cinema.phoneNumber ?? '',
      email: cinema.email ?? '',
      logoUrl: cinema.logoUrl ?? '',
      openTime: cinema.openTime,
      closeTime: cinema.closeTime,
      isActive: cinema.isActive,
    });
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('cinemas.confirmDelete'))) return;
    try {
      await cinemaApi.delete(id);
      await loadCinemas();
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to delete cinema'));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (editingId) {
        const updateData: UpdateCinemaDto = {
          name: form.name,
          address: form.address,
          city: form.city,
          country: form.country,
          phoneNumber: form.phoneNumber || null,
          email: form.email || null,
          logoUrl: form.logoUrl || null,
          openTime: form.openTime,
          closeTime: form.closeTime,
          isActive: form.isActive,
        };
        await cinemaApi.update(editingId, updateData);
      } else {
        const createData: CreateCinemaDto = {
          name: form.name,
          address: form.address,
          city: form.city,
          country: form.country,
          phoneNumber: form.phoneNumber || null,
          email: form.email || null,
          logoUrl: form.logoUrl || null,
          openTime: form.openTime,
          closeTime: form.closeTime,
        };
        await cinemaApi.create(createData);
      }
      setShowForm(false);
      setForm(EMPTY_FORM);
      setEditingId(null);
      await loadCinemas();
    } catch (err: unknown) {
      setError(extractErrorMessage(err, editingId ? 'Failed to update cinema' : 'Failed to create cinema'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="page"><div className="loading">{t('common.loading')}</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div className="page-header">
          <h1>{t('cinemas.title')}</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + {t('cinemas.addCinema')}
          </button>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal modal-lg" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? t('cinemas.editCinema') : t('cinemas.addCinema')}</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="name">{t('cinemas.form.name')}</label>
                  <input id="name" name="name" value={form.name} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-group">
                  <label htmlFor="address">{t('cinemas.form.address')}</label>
                  <input id="address" name="address" value={form.address} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="city">{t('cinemas.form.city')}</label>
                    <input id="city" name="city" value={form.city} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="country">{t('cinemas.form.country')}</label>
                    <input id="country" name="country" value={form.country} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="phoneNumber">{t('cinemas.form.phone')}</label>
                    <input id="phoneNumber" name="phoneNumber" value={form.phoneNumber} onChange={handleInputChange} className="input" />
                  </div>
                  <div className="form-group">
                    <label htmlFor="email">{t('cinemas.form.email')}</label>
                    <input id="email" name="email" type="email" value={form.email} onChange={handleInputChange} className="input" />
                  </div>
                </div>
                <div className="form-group">
                  <label htmlFor="logoUrl">{t('cinemas.form.logoUrl')}</label>
                  <input id="logoUrl" name="logoUrl" value={form.logoUrl} onChange={handleInputChange} className="input" placeholder="https://..." />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="openTime">{t('cinemas.form.openTime')}</label>
                    <input id="openTime" name="openTime" type="time" value={form.openTime} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="closeTime">{t('cinemas.form.closeTime')}</label>
                    <input id="closeTime" name="closeTime" type="time" value={form.closeTime} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                {editingId && (
                  <div className="form-group form-check">
                    <label>
                      <input type="checkbox" name="isActive" checked={form.isActive} onChange={handleInputChange} />
                      {' '}{t('common.active')}
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
                <th>{t('cinemas.columns.name')}</th>
                <th>{t('cinemas.columns.location')}</th>
                <th>{t('cinemas.columns.contact')}</th>
                <th>{t('cinemas.columns.hours')}</th>
                <th>{t('cinemas.columns.halls')}</th>
                <th>{t('cinemas.columns.status')}</th>
                <th>{t('cinemas.columns.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {cinemas.map((cinema) => (
                <tr key={cinema.id}>
                  <td>
                    <strong>{cinema.name}</strong>
                    {cinema.logoUrl && (
                      <img src={cinema.logoUrl} alt="" style={{ width: 24, height: 24, marginLeft: 8, objectFit: 'contain' }} />
                    )}
                  </td>
                  <td>{cinema.city}, {cinema.country}</td>
                  <td>
                    {cinema.email && <div>{cinema.email}</div>}
                    {cinema.phoneNumber && <div>{cinema.phoneNumber}</div>}
                  </td>
                  <td>{cinema.openTime} – {cinema.closeTime}</td>
                  <td>{cinema.hallCount}</td>
                  <td>
                    <span className={`status-badge status-${cinema.isActive ? 'confirmed' : 'cancelled'}`}>
                      {cinema.isActive ? t('common.active') : t('common.inactive')}
                    </span>
                  </td>
                  <td>
                    <div className="table-actions">
                      <button onClick={() => handleEdit(cinema)} className="btn btn-sm btn-outline">{t('common.edit')}</button>
                      <button onClick={() => handleDelete(cinema.id)} className="btn btn-sm btn-danger">{t('common.delete')}</button>
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
