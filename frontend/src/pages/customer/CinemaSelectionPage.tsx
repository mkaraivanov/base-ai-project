import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto } from '../../types';

export const CinemaSelectionPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadCinemas = async () => {
      try {
        const data = await cinemaApi.getAll(true);
        setCinemas(data);
      } catch {
        setError(t('cinemaSelection.error'));
      } finally {
        setLoading(false);
      }
    };
    loadCinemas();
  }, [t]);

  return (
    <div className="page home-page">
      <section className="hero">
        <div className="hero-content">
          <h1>{t('cinemaSelection.welcome')}</h1>
          <p>{t('cinemaSelection.heroSubtitle')}</p>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <h2>{t('cinemaSelection.title')}</h2>
          {loading ? (
            <div className="loading">{t('cinemaSelection.loading')}</div>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : cinemas.length === 0 ? (
            <p className="empty-state">{t('cinemaSelection.noCinemasAtMoment')}</p>
          ) : (
            <div className="cinema-grid">
              {cinemas.map((cinema) => (
                <Link
                  key={cinema.id}
                  to={`/cinemas/${cinema.id}/movies`}
                  className="cinema-card"
                >
                  {cinema.logoUrl ? (
                    <img
                      src={cinema.logoUrl}
                      alt={cinema.name}
                      className="cinema-card-logo"
                    />
                  ) : (
                    <div className="cinema-card-icon">🏛️</div>
                  )}
                  <h3>{cinema.name}</h3>
                  <p className="cinema-location">
                    {cinema.city}, {cinema.country}
                  </p>
                  <p className="cinema-address">{cinema.address}</p>
                  <p className="cinema-hours">
                    {cinema.openTime} – {cinema.closeTime}
                  </p>
                  <p className="cinema-halls">
                    {t('cinemaSelection.halls', { count: cinema.hallCount })}
                  </p>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
};
