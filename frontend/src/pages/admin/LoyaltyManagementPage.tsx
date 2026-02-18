import React, { useEffect, useState } from 'react';
import { loyaltyApi } from '../../api/loyaltyApi';
import { extractErrorMessage } from '../../utils/errorHandler';

export const LoyaltyManagementPage: React.FC = () => {
  const [stampsRequired, setStampsRequired] = useState<number>(5);
  const [inputValue, setInputValue] = useState<string>('5');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const settings = await loyaltyApi.getSettings();
        setStampsRequired(settings.stampsRequired);
        setInputValue(String(settings.stampsRequired));
      } catch (err: unknown) {
        setError(extractErrorMessage(err, 'Failed to load loyalty settings'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    const parsed = parseInt(inputValue, 10);
    if (isNaN(parsed) || parsed < 1) {
      setError('Number of visits must be at least 1');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const updated = await loyaltyApi.updateSettings({ stampsRequired: parsed });
      setStampsRequired(updated.stampsRequired);
      setInputValue(String(updated.stampsRequired));
      setSuccess('Loyalty settings saved successfully.');
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to save loyalty settings'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;

  return (
    <div className="page">
      <div className="container">
        <h1>🎁 Loyalty Program Settings</h1>
        <p style={{ color: 'var(--color-text-secondary)', marginBottom: '32px' }}>
          Configure how many paid movie visits a customer needs to earn a free ticket.
          Multiple tickets in one transaction count as a single visit.
        </p>

        <div className="form-card" style={{
          background: 'var(--color-surface)',
          borderRadius: 'var(--radius-lg)',
          padding: '32px',
          boxShadow: 'var(--shadow)',
          maxWidth: '480px',
        }}>
          <div className="form-group" style={{ marginBottom: '24px' }}>
            <label htmlFor="stampsRequired" style={{ fontWeight: 600, display: 'block', marginBottom: '8px' }}>
              Visits required for a free ticket
            </label>
            <input
              id="stampsRequired"
              type="number"
              min={1}
              max={100}
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              className="form-input"
              style={{
                width: '100%',
                padding: '10px 14px',
                border: '1px solid var(--color-border)',
                borderRadius: 'var(--radius)',
                fontSize: '1rem',
              }}
            />
            <p style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginTop: '6px' }}>
              Currently set to <strong>{stampsRequired}</strong> visits.
            </p>
          </div>

          {error && (
            <div className="error-message" style={{ marginBottom: '16px' }}>
              {error}
            </div>
          )}
          {success && (
            <div style={{
              background: '#dcfce7',
              color: '#166534',
              borderRadius: 'var(--radius)',
              padding: '10px 14px',
              marginBottom: '16px',
              fontSize: '0.9rem',
            }}>
              {success}
            </div>
          )}

          <button
            onClick={handleSave}
            disabled={saving}
            className="btn btn-primary"
            style={{ width: '100%' }}
          >
            {saving ? 'Saving...' : 'Save Settings'}
          </button>
        </div>

        <div style={{ marginTop: '40px', maxWidth: '480px' }}>
          <h2 style={{ fontSize: '1.1rem', fontWeight: 600, marginBottom: '12px' }}>How it works</h2>
          <ul style={{ paddingLeft: '20px', lineHeight: '2', color: 'var(--color-text-secondary)' }}>
            <li>Each qualifying booking awards the customer 1 stamp.</li>
            <li>A booking qualifies if payment is successful and the reservation is not cancelled or refunded.</li>
            <li>Multiple tickets in one transaction count as one visit.</li>
            <li>When a customer reaches the required number of stamps, they automatically receive a free ticket voucher.</li>
            <li>The stamp counter resets after each reward is issued.</li>
          </ul>
        </div>
      </div>
    </div>
  );
};
