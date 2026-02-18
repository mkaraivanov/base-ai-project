import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto } from '../../types';

export const CinemaSelectionPage: React.FC = () => {
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadCinemas = async () => {
      try {
        const data = await cinemaApi.getAll(true);
        setCinemas(data);
      } catch {
        setError('Failed to load cinemas');
      } finally {
        setLoading(false);
      }
    };
    loadCinemas();
  }, []);

  return (
    <div className="page home-page">
      <section className="hero">
        <div className="hero-content">
          <h1>Welcome to CineBook</h1>
          <p>Pick your nearest cinema and book tickets in seconds.</p>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <h2>Select a Cinema</h2>
          {loading ? (
            <div className="loading">Loading cinemas...</div>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : cinemas.length === 0 ? (
            <p className="empty-state">No cinemas available at the moment.</p>
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
                    {cinema.hallCount} hall{cinema.hallCount !== 1 ? 's' : ''}
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
