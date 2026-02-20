import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { loyaltyApi } from '../../api/loyaltyApi';
import { extractErrorMessage } from '../../utils/errorHandler';

export const LoyaltyManagementPage: React.FC = () => {
  const { t } = useTranslation('admin');
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
        setError(extractErrorMessage(err, t('loyalty.failedToLoad')));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    const parsed = parseInt(inputValue, 10);
    if (isNaN(parsed) || parsed < 1) {
      setError(t('loyalty.invalidVisits'));
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const updated = await loyaltyApi.updateSettings({ stampsRequired: parsed });
      setStampsRequired(updated.stampsRequired);
      setInputValue(String(updated.stampsRequired));
      setSuccess(t('loyalty.savedSuccess'));
    } catch (err: unknown) {
      setError(extractErrorMessage(err, t('loyalty.failedToSave')));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="page"><div className="loading">{t('common.loading')}</div></div>;

  return (
    <div className="page">
      <div className="container">
        <h1>{t('loyalty.pageTitle')}</h1>
        <p style={{ color: 'var(--color-text-secondary)', marginBottom: '32px' }}>
          {t('loyalty.pageSubtitle')}
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
              {t('loyalty.visitsRequired')}
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
            <p style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary)', marginTop: '6px' }}
              dangerouslySetInnerHTML={{ __html: t('loyalty.currentlySet', { count: stampsRequired }) }}
            />
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
            {saving ? t('loyalty.saving') : t('loyalty.saveSettings')}
          </button>
        </div>

        <div style={{ marginTop: '40px', maxWidth: '480px' }}>
          <h2 style={{ fontSize: '1.1rem', fontWeight: 600, marginBottom: '12px' }}>{t('loyalty.howItWorks')}</h2>
          <ul style={{ paddingLeft: '20px', lineHeight: '2', color: 'var(--color-text-secondary)' }}>
            <li>{t('loyalty.bullet1')}</li>
            <li>{t('loyalty.bullet2')}</li>
            <li>{t('loyalty.bullet3')}</li>
            <li>{t('loyalty.bullet4')}</li>
            <li>{t('loyalty.bullet5')}</li>
          </ul>
        </div>
      </div>
    </div>
  );
};
