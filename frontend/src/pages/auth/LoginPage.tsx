import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { extractErrorMessage } from '../../utils/errorHandler';

export const LoginPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setError(t('login.fillAllFields'));
      return;
    }

    try {
      setLoading(true);
      setError(null);
      await login(email, password);
      navigate('/');
    } catch (err: unknown) {
      const message = extractErrorMessage(err, t('login.error'));
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page auth-page">
      <div className="container container-xs">
        <div className="auth-card">
          <h1>{t('login.title')}</h1>
          <p className="auth-subtitle">{t('login.subtitle')}</p>

          {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

          <form onSubmit={handleSubmit} className="form">
            <div className="form-group">
              <label htmlFor="email">{t('login.email')}</label>
              <input
                type="email"
                id="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t('login.emailPlaceholder')}
                className="input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">{t('login.password')}</label>
              <input
                type="password"
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t('register.passwordPlaceholder')}
                className="input"
                required
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary btn-full"
              disabled={loading}
            >
              {loading ? t('login.signingIn') : t('login.submit')}
            </button>
          </form>

          <p className="auth-footer">
            {t('login.noAccount')}{' '}
            <Link to="/register">{t('login.signUpLink')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
};
