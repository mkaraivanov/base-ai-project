import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import type { RegisterDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';

export const RegisterPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '',
  });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (form.password !== form.confirmPassword) {
      setError(t('register.passwordMismatch'));
      return;
    }

    if (form.password.length < 6) {
      setError(t('register.passwordTooShort'));
      return;
    }

    try {
      setLoading(true);
      const registerData: RegisterDto = {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        password: form.password,
        phoneNumber: form.phoneNumber,
      };
      await register(registerData);
      navigate('/');
    } catch (err: unknown) {
      const message = extractErrorMessage(err, t('register.failed'));
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page auth-page">
      <div className="container container-xs">
        <div className="auth-card">
          <h1>{t('register.title')}</h1>
          <p className="auth-subtitle">{t('register.subtitle')}</p>

          {error && (
            <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>
          )}

          <form onSubmit={handleSubmit} className="form">
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="firstName">{t('register.firstName')}</label>
                <input
                  type="text"
                  id="firstName"
                  name="firstName"
                  value={form.firstName}
                  onChange={handleChange}
                  className="input"
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="lastName">{t('register.lastName')}</label>
                <input
                  type="text"
                  id="lastName"
                  name="lastName"
                  value={form.lastName}
                  onChange={handleChange}
                  className="input"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="email">{t('register.email')}</label>
              <input
                type="email"
                id="email"
                name="email"
                value={form.email}
                onChange={handleChange}
                placeholder={t('register.emailPlaceholder')}
                className="input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="phoneNumber">{t('register.phone')}</label>
              <input
                type="tel"
                id="phoneNumber"
                name="phoneNumber"
                value={form.phoneNumber}
                onChange={handleChange}
                placeholder={t('register.phonePlaceholder')}
                className="input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">{t('register.password')}</label>
              <input
                type="password"
                id="password"
                name="password"
                value={form.password}
                onChange={handleChange}
                placeholder={t('register.passwordPlaceholder')}
                className="input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="confirmPassword">{t('register.confirmPassword')}</label>
              <input
                type="password"
                id="confirmPassword"
                name="confirmPassword"
                value={form.confirmPassword}
                onChange={handleChange}
                placeholder="••••••••"
                className="input"
                required
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary btn-full"
              disabled={loading}
            >
              {loading ? t('register.creatingAccount') : t('register.submit')}
            </button>
          </form>

          <p className="auth-footer">
            {t('register.hasAccount')}{' '}
            <Link to="/login">{t('register.loginLink')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
};
