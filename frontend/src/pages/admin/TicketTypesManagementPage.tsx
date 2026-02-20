import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation('admin');
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
      setError(t('ticketTypes.failedToLoad'));
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
    if (!window.confirm(t('ticketTypes.confirmDeactivate'))) return;
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
      setError(t('ticketTypes.invalidModifier'));
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
      setForm(EMPTY_FORM);
      setEditingId(null);
      await loadTicketTypes();
    } catch (err: unknown) {
      setError(extractErrorMessage(err, t('ticketTypes.failedToSave')));
    } finally {
      setSaving(false);
    }
  };

  const formatModifier = (modifier: number): string => {
    const pct = Math.round(modifier * 100);
    if (modifier === 1.0) return `×1.00 (full price)`;
    return `×${modifier.toFixed(2)} (${pct}% of seat price)`;
  };

  if (loading) return <div className="page"><div className="loading">{t('common.loading')}</div></div>;

  return (
    <div className="page">
      <div className="container">
        <div className="page-header">
          <h1>{t('ticketTypes.title')}</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + {t('ticketTypes.addType')}
          </button>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={handleCancel}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? t('ticketTypes.editType') : t('ticketTypes.addType')}</h2>
              <form onSubmit={handleSubmit} className="form">
                <div className="form-group">
                  <label htmlFor="name">{t('ticketTypes.form.name')}</label>
                  <input
                    id="name"
                    name="name"
                    className="input"
                    value={form.name}
                    onChange={handleInputChange}
                    required
                    placeholder={t('ticketTypes.form.namePlaceholder')}
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="description">{t('ticketTypes.form.description')}</label>
                  <input
                    id="description"
                    name="description"
                    className="input"
                    value={form.description}
                    onChange={handleInputChange}
                    placeholder={t('ticketTypes.form.descriptionPlaceholder')}
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="priceModifier">
                    {t('ticketTypes.form.priceModifier')}{' '}
                    <small style={{ fontWeight: 'normal', color: '#666' }}>
                      {t('ticketTypes.form.priceModifierHint')}
                    </small>
                  </label>
                  <input
                    id="priceModifier"
                    name="priceModifier"
                    type="number"
                    className="input"
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
                  <label htmlFor="sortOrder">{t('ticketTypes.form.sortOrder')}</label>
                  <input
                    id="sortOrder"
                    name="sortOrder"
                    type="number"
                    className="input"
                    value={form.sortOrder}
                    onChange={handleInputChange}
                    style={{ maxWidth: '100px' }}
                  />
                </div>
                {editingId && (
                  <div className="form-group form-check">
                    <label>
                      <input
                        name="isActive"
                        type="checkbox"
                        checked={form.isActive}
                        onChange={handleInputChange}
                      />
                      {t('common.active')}
                    </label>
                  </div>
                )}
                <div className="form-actions">
                  <button type="button" className="btn btn-outline" onClick={handleCancel}>{t('common.cancel')}</button>
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
                <th>{t('ticketTypes.columns.name')}</th>
                <th>{t('ticketTypes.columns.description')}</th>
                <th>{t('ticketTypes.columns.modifier')}</th>
                <th>{t('ticketTypes.columns.sortOrder')}</th>
                <th>{t('ticketTypes.columns.status')}</th>
                <th>{t('ticketTypes.columns.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {ticketTypes.length === 0 ? (
                <tr><td colSpan={6}>No ticket types found.</td></tr>
              ) : (
                ticketTypes.map((tt) => (
                  <tr key={tt.id}>
                    <td><strong>{tt.name}</strong></td>
                    <td>{tt.description || '—'}</td>
                    <td>{formatModifier(tt.priceModifier)}</td>
                    <td>{tt.sortOrder}</td>
                    <td>
                      <span className={`status-badge status-${tt.isActive ? 'confirmed' : 'cancelled'}`}>
                        {tt.isActive ? t('common.active') : t('common.inactive')}
                      </span>
                    </td>
                    <td>
                      <div className="table-actions">
                        <button className="btn btn-sm btn-outline" onClick={() => handleEdit(tt)}>{t('common.edit')}</button>
                        <button className="btn btn-sm btn-danger" onClick={() => handleDelete(tt.id)}>{t('common.delete')}</button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
