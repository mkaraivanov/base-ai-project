import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { hallApi } from '../../api/hallApi';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto, CinemaHallDto, CreateCinemaHallDto, UpdateCinemaHallDto, SeatLayout, SeatDefinition } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';

interface HallFormData {
  cinemaId: string;
  name: string;
  rows: string;
  seatsPerRow: string;
  isActive: boolean;
}

const EMPTY_FORM: HallFormData = {
  cinemaId: '',
  name: '',
  rows: '8',
  seatsPerRow: '10',
  isActive: true,
};

const generateSeatLayout = (rows: number, seatsPerRow: number): SeatLayout => {
  const seats: SeatDefinition[] = [];
  for (let r = 0; r < rows; r++) {
    const rowLetter = String.fromCharCode(65 + r);
    for (let c = 1; c <= seatsPerRow; c++) {
      const seatType = r >= rows - 2 ? 'Premium' : 'Regular';
      seats.push({
        seatNumber: `${rowLetter}${c}`,
        row: r + 1,
        column: c,
        seatType,
        priceMultiplier: seatType === 'Premium' ? 1.5 : 1.0,
        isAvailable: true,
      });
    }
  }
  return { rows, seatsPerRow, seats };
};

export const HallsManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
  const [halls, setHalls] = useState<readonly CinemaHallDto[]>([]);
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [filterCinemaId, setFilterCinemaId] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<HallFormData>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadCinemas = async () => {
    try {
      const data = await cinemaApi.getAll(true);
      setCinemas(data);
    } catch {
      // non-blocking
    }
  };

  const loadHalls = async () => {
    try {
      setLoading(true);
      const data = await hallApi.getAll(false, filterCinemaId || undefined);
      setHalls(data);
    } catch {
      setError('Failed to load halls');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCinemas();
  }, []);

  useEffect(() => {
    loadHalls();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterCinemaId]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    if (type === 'checkbox') {
      setForm((prev) => ({ ...prev, [name]: (e.target as HTMLInputElement).checked }));
    } else {
      setForm((prev) => ({ ...prev, [name]: value }));
    }
  };

  const handleCreate = () => {
    setEditingId(null);
    setForm({ ...EMPTY_FORM, cinemaId: cinemas[0]?.id ?? '' });
    setShowForm(true);
    setError(null);
  };

  const handleEdit = (hall: CinemaHallDto) => {
    setEditingId(hall.id);
    setForm({
      cinemaId: hall.cinemaId,
      name: hall.name,
      rows: hall.seatLayout.rows.toString(),
      seatsPerRow: hall.seatLayout.seatsPerRow.toString(),
      isActive: hall.isActive,
    });
    setShowForm(true);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(t('halls.confirmDelete'))) return;
    try {
      await hallApi.delete(id);
      await loadHalls();
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to delete hall');
      setError(message);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    const rows = parseInt(form.rows, 10);
    const seatsPerRow = parseInt(form.seatsPerRow, 10);
    const seatLayout = generateSeatLayout(rows, seatsPerRow);

    try {
      if (editingId) {
        const updateData: UpdateCinemaHallDto = {
          name: form.name,
          seatLayout,
          isActive: form.isActive,
        };
        await hallApi.update(editingId, updateData);
      } else {
        const createData: CreateCinemaHallDto = {
          cinemaId: form.cinemaId,
          name: form.name,
          seatLayout,
        };
        await hallApi.create(createData);
      }
      setShowForm(false);
      setForm(EMPTY_FORM);
      setEditingId(null);
      await loadHalls();
    } catch (err: unknown) {
      const message = extractErrorMessage(
        err,
        editingId ? 'Failed to update hall' : 'Failed to create hall'
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
          <h1>{t('halls.title')}</h1>
          <button onClick={handleCreate} className="btn btn-primary">
            + {t('halls.addHall')}
          </button>
        </div>

        <div className="filters">
          <select
            value={filterCinemaId}
            onChange={(e) => setFilterCinemaId(e.target.value)}
            className="input"
            style={{ maxWidth: 260 }}
          >
            <option value="">{t('common.allCinemas')}</option>
            {cinemas.map((c) => (
              <option key={c.id} value={c.id}>{c.name} – {c.city}</option>
            ))}
          </select>
        </div>

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {showForm && (
          <div className="modal-overlay" onClick={() => setShowForm(false)}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2>{editingId ? t('halls.editHall') : t('halls.addHall')}</h2>
              <form onSubmit={handleSubmit} className="form">
                {!editingId && (
                  <div className="form-group">
                    <label htmlFor="cinemaId">{t('halls.form.cinema')}</label>
                    <select id="cinemaId" name="cinemaId" value={form.cinemaId} onChange={handleInputChange} className="input" required>
                      <option value="">{t('halls.form.selectCinema')}</option>
                      {cinemas.map((c) => (
                        <option key={c.id} value={c.id}>{c.name} – {c.city}</option>
                      ))}
                    </select>
                  </div>
                )}
                <div className="form-group">
                  <label htmlFor="name">{t('halls.form.name')}</label>
                  <input id="name" name="name" value={form.name} onChange={handleInputChange} className="input" required />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="rows">{t('halls.form.rows')}</label>
                    <input id="rows" name="rows" type="number" min="1" max="26" value={form.rows} onChange={handleInputChange} className="input" required />
                  </div>
                  <div className="form-group">
                    <label htmlFor="seatsPerRow">{t('halls.form.seatsPerRow')}</label>
                    <input id="seatsPerRow" name="seatsPerRow" type="number" min="1" max="30" value={form.seatsPerRow} onChange={handleInputChange} className="input" required />
                  </div>
                </div>
                <p className="form-help">{t('halls.form.totalSeats', { count: parseInt(form.rows || '0', 10) * parseInt(form.seatsPerRow || '0', 10) })}</p>
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
                <th>{t('halls.columns.cinema')}</th>
                <th>{t('halls.columns.name')}</th>
                <th>{t('halls.columns.totalSeats')}</th>
                <th>{t('halls.columns.layout')}</th>
                <th>{t('halls.columns.status')}</th>
                <th>{t('halls.columns.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {halls.map((hall) => (
                <tr key={hall.id}>
                  <td>{hall.cinemaName}</td>
                  <td>{hall.name}</td>
                  <td>{hall.totalSeats}</td>
                  <td>{hall.seatLayout.rows} x {hall.seatLayout.seatsPerRow}</td>
                  <td>
                    <span className={`status-badge status-${hall.isActive ? 'confirmed' : 'cancelled'}`}>
                      {hall.isActive ? t('common.active') : t('common.inactive')}
                    </span>
                  </td>
                  <td>
                    <div className="table-actions">
                      <button onClick={() => handleEdit(hall)} className="btn btn-sm btn-outline">{t('common.edit')}</button>
                      <button onClick={() => handleDelete(hall.id)} className="btn btn-sm btn-danger">{t('common.delete')}</button>
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
