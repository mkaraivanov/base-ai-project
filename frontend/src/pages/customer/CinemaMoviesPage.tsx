import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cinemaApi } from '../../api/cinemaApi';
import { showtimeApi } from '../../api/showtimeApi';
import { movieApi } from '../../api/movieApi';
import { MovieCard } from '../../components/MovieCard/MovieCard';
import type { CinemaDto, MovieDto } from '../../types';

export const CinemaMoviesPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const { cinemaId } = useParams<{ cinemaId: string }>();
  const [cinema, setCinema] = useState<CinemaDto | null>(null);
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      if (!cinemaId) return;
      try {
        setLoading(true);
        const [cinemaData, allMovies, showtimes] = await Promise.all([
          cinemaApi.getById(cinemaId),
          movieApi.getAll(true),
          showtimeApi.getAll(undefined, undefined, cinemaId),
        ]);
        setCinema(cinemaData);

        // Filter movies that have at least one active showtime at this cinema
        const movieIdsWithShowtimes = new Set(showtimes.map((s) => s.movieId));
        setMovies(allMovies.filter((m) => movieIdsWithShowtimes.has(m.id)));
      } catch {
        setError(t('cinemaMovies.error'));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [cinemaId, t]);

  return (
    <div className="page">
      <div className="container">
        <div className="page-breadcrumb">
          <Link to="/">{t('cinemaMovies.allCinemas')}</Link>
        </div>

        {cinema && (
          <div className="cinema-header">
            {cinema.logoUrl && (
              <img src={cinema.logoUrl} alt={cinema.name} className="cinema-header-logo" />
            )}
            <div>
              <h1>{cinema.name}</h1>
              <p className="cinema-location">
                {cinema.address}, {cinema.city}, {cinema.country}
              </p>
              <p className="cinema-hours">
                Open: {cinema.openTime} – {cinema.closeTime}
              </p>
            </div>
          </div>
        )}

        <section className="section">
          <h2>{t('cinemaMovies.nowShowing')}</h2>
          {loading ? (
            <div className="loading">{t('cinemaMovies.loading')}</div>
          ) : error ? (
            <p className="error-message">{error}</p>
          ) : movies.length === 0 ? (
            <p className="empty-state">{t('cinemaMovies.noMovies')}</p>
          ) : (
            <div className="movie-grid">
              {movies.map((movie) => (
                <MovieCard
                  key={movie.id}
                  movie={movie}
                  detailPath={`/cinemas/${cinemaId}/movies/${movie.id}`}
                />
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
};
