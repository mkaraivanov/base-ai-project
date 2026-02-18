import React, { useEffect, useState } from 'react';
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
    if (!window.confirm('Are you sure you want to delete this cinema? All associated halls must be inactive first.')) return;
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

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div className="page-header">
          <h1>Cinemas Management</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + Add Cinema
          </button>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal modal-lg" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? 'Edit Cinema' : 'Add Cinema'}</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="name">Cinema Name *</label>
                  <input id="name" name="name" value={form.name} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-group">
                  <label htmlFor="address">Address *</label>
                  <input id="address" name="address" value={form.address} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="city">City *</label>
                    <input id="city" name="city" value={form.city} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="country">Country *</label>
                    <input id="country" name="country" value={form.country} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="phoneNumber">Phone Number</label>
                    <input id="phoneNumber" name="phoneNumber" value={form.phoneNumber} onChange={handleInputChange} className="input" />
                  </div>
                  <div className="form-group">
                    <label htmlFor="email">Email</label>
                    <input id="email" name="email" type="email" value={form.email} onChange={handleInputChange} className="input" />
                  </div>
                </div>
                <div className="form-group">
                  <label htmlFor="logoUrl">Logo URL</label>
                  <input id="logoUrl" name="logoUrl" value={form.logoUrl} onChange={handleInputChange} className="input" placeholder="https://..." />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="openTime">Opening Time *</label>
                    <input id="openTime" name="openTime" type="time" value={form.openTime} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="closeTime">Closing Time *</label>
                    <input id="closeTime" name="closeTime" type="time" value={form.closeTime} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                {editingId && (
                  <div className="form-group form-check">
                    <label>
                      <input type="checkbox" name="isActive" checked={form.isActive} onChange={handleInputChange} />
                      {' '}Active
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
                <th>Name</th>
                <th>Location</th>
                <th>Contact</th>
                <th>Hours</th>
                <th>Halls</th>
                <th>Status</th>
                <th>Actions</th>
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
                      {cinema.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div className="table-actions">
                      <button onClick={() => handleEdit(cinema)} className="btn btn-sm btn-outline">Edit</button>
                      <button onClick={() => handleDelete(cinema.id)} className="btn btn-sm btn-danger">Delete</button>
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
