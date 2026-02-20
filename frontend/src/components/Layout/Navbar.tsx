import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { LanguageSwitcher } from '../LanguageSwitcher/LanguageSwitcher';

export const Navbar: React.FC = () => {
  const { isAuthenticated, isAdmin, user, logout } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation('common');

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <nav className="navbar">
      <div className="navbar-container">
        <Link to="/" className="navbar-brand">
          🎬 CineBook
        </Link>

        <div className="navbar-links">
          {isAuthenticated && (
            <Link to="/movies" className="nav-link">
              {t('nav.movies')}
            </Link>
          )}

          {isAuthenticated && (
            <Link to="/my-bookings" className="nav-link">
              {t('nav.myTickets')}
            </Link>
          )}

          {isAdmin && (
            <Link to="/admin" className="nav-link nav-link-admin">
              {t('nav.admin')}
            </Link>
          )}
        </div>

        <div className="navbar-controls">
          <LanguageSwitcher />
        </div>

        <div className="navbar-auth">
          {isAuthenticated ? (
            <div className="user-menu">
              <span className="user-greeting">
                {t('nav.greeting', { name: user?.firstName })}
              </span>
              <button onClick={handleLogout} className="btn btn-outline">
                {t('nav.logout')}
              </button>
            </div>
          ) : (
            <div className="auth-buttons">
              <Link to="/login" className="btn btn-outline">
                {t('nav.login')}
              </Link>
              <Link to="/register" className="btn btn-primary">
                {t('nav.signUp')}
              </Link>
            </div>
          )}
        </div>
      </div>
    </nav>
  );
};
