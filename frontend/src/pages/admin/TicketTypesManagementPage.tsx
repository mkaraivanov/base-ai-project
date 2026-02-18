import React, { useEffect, useState } from 'react';
import { ticketTypeApi } from '../../api/ticketTypeApi';
import type { TicketTypeDto, CreateTicketTypeDto, UpdateTicketTypeDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';

interface TicketTypeFormData {
  name: string;
  description: string;
  priceModifier: string;
  sortOrder: string;
  isActive: boolean;
}

const EMPTY_FORM: TicketTypeFormData = {
  name: '',
  description: '',
  priceModifier: '1.00',
  sortOrder: '0',
  isActive: true,
};

export const TicketTypesManagementPage: React.FC = () => {
  const [ticketTypes, setTicketTypes] = useState<readonly TicketTypeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<TicketTypeFormData>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadTicketTypes = async () => {
    try {
      setLoading(true);
      const data = await ticketTypeApi.getAll();
      setTicketTypes(data);
    } catch {
      setError('Failed to load ticket types');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTicketTypes();
  }, []);

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
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

  const handleEdit = (tt: TicketTypeDto) => {
    setEditingId(tt.id);
    setForm({
      name: tt.name,
      description: tt.description,
      priceModifier: tt.priceModifier.toFixed(2),
      sortOrder: tt.sortOrder.toString(),
      isActive: tt.isActive,
    });
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to deactivate this ticket type?')) return;
    try {
      await ticketTypeApi.delete(id);
      await loadTicketTypes();
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to delete ticket type'));
    }
  };

  const handleCancel = () => {
    setShowForm(false);
    setEditingId(null);
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    const modifier = parseFloat(form.priceModifier);
    if (isNaN(modifier) || modifier <= 0) {
      setError('Price modifier must be a positive number');
      setSaving(false);
      return;
    }

    try {
      if (editingId) {
        const dto: UpdateTicketTypeDto = {
          name: form.name,
          description: form.description,
          priceModifier: modifier,
          isActive: form.isActive,
          sortOrder: parseInt(form.sortOrder, 10) || 0,
        };
        await ticketTypeApi.update(editingId, dto);
      } else {
        const dto: CreateTicketTypeDto = {
          name: form.name,
          description: form.description,
          priceModifier: modifier,
          sortOrder: parseInt(form.sortOrder, 10) || 0,
        };
        await ticketTypeApi.create(dto);
      }

      setShowForm(false);
      setEditingId(null);
      await loadTicketTypes();
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to save ticket type'));
    } finally {
      setSaving(false);
    }
  };

  const formatModifier = (modifier: number): string => {
    const pct = Math.round(modifier * 100);
    if (modifier === 1.0) return `×1.00 (full price)`;
    return `×${modifier.toFixed(2)} (${pct}% of seat price)`;
  };

  if (loading) return <div className="page"><div className="loading">Loading ticket types...</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h1>Ticket Types</h1>
          {!showForm && (
            <button onClick={handleCreate} className="btn btn-primary">
              + Add Ticket Type
            </button>
          )}
        </div>

        {error && <div className="error-message">{error}</div>}

        {showForm && (
          <div className="form-container" style={{ marginBottom: '2rem', padding: '1.5rem', border: '1px solid #ddd', borderRadius: '8px' }}>
            <h2>{editingId ? 'Edit Ticket Type' : 'Create Ticket Type'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Name *</label>
                <input
                  name="name"
                  className="form-control"
                  value={form.name}
                  onChange={handleInputChange}
                  required
                  placeholder="e.g. Adult, Children, Senior"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Description</label>
                <input
                  name="description"
                  className="form-control"
                  value={form.description}
                  onChange={handleInputChange}
                  placeholder="e.g. Children aged 12 and under — 50% discount"
                />
              </div>
              <div className="form-group">
                <label className="form-label">
                  Price Modifier *{' '}
                  <small style={{ fontWeight: 'normal', color: '#666' }}>
                    (1.0 = full price, 0.5 = 50% off, 1.2 = 20% surcharge)
                  </small>
                </label>
                <input
                  name="priceModifier"
                  type="number"
                  className="form-control"
                  value={form.priceModifier}
                  onChange={handleInputChange}
                  min="0.01"
                  max="5"
                  step="0.01"
                  required
                  style={{ maxWidth: '160px' }}
                />
                {form.priceModifier && !isNaN(parseFloat(form.priceModifier)) && (
                  <small style={{ display: 'block', marginTop: '4px', color: '#555' }}>
                    {formatModifier(parseFloat(form.priceModifier))}
                  </small>
                )}
              </div>
              <div className="form-group">
                <label className="form-label">Sort Order</label>
                <input
                  name="sortOrder"
                  type="number"
                  className="form-control"
                  value={form.sortOrder}
                  onChange={handleInputChange}
                  style={{ maxWidth: '100px' }}
                />
              </div>
              {editingId && (
                <div className="form-group">
                  <label style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <input
                      name="isActive"
                      type="checkbox"
                      checked={form.isActive}
                      onChange={handleInputChange}
                    />
                    Active
                  </label>
                </div>
              )}
              <div style={{ display: 'flex', gap: '1rem' }}>
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving ? 'Saving...' : editingId ? 'Update' : 'Create'}
                </button>
                <button type="button" className="btn btn-secondary" onClick={handleCancel}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}

        {ticketTypes.length === 0 ? (
          <p>No ticket types found. Create one above.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Price Modifier</th>
                <th>Sort Order</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {ticketTypes.map((tt) => (
                <tr key={tt.id}>
                  <td><strong>{tt.name}</strong></td>
                  <td>{tt.description || '—'}</td>
                  <td>{formatModifier(tt.priceModifier)}</td>
                  <td>{tt.sortOrder}</td>
                  <td>
                    <span className={`badge ${tt.isActive ? 'badge-success' : 'badge-secondary'}`}>
                      {tt.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => handleEdit(tt)}
                      style={{ marginRight: '0.5rem' }}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => handleDelete(tt.id)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
