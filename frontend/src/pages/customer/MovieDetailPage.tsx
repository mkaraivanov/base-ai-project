import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { movieApi } from '../../api/movieApi';
import { showtimeApi } from '../../api/showtimeApi';
import type { MovieDto, ShowtimeDto } from '../../types';
import { formatDate, formatDateTime, formatDuration, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

export const MovieDetailPage: React.FC = () => {
  const { movieId } = useParams<{ movieId: string }>();
  const [movie, setMovie] = useState<MovieDto | null>(null);
  const [showtimes, setShowtimes] = useState<readonly ShowtimeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      if (!movieId) return;
      try {
        setLoading(true);
        const [movieData, showtimeData] = await Promise.all([
          movieApi.getById(movieId),
          showtimeApi.getByMovie(movieId),
        ]);
        setMovie(movieData);
        setShowtimes(showtimeData);
      } catch (err: unknown) {
        const message = extractErrorMessage(err, 'Failed to load movie details');
        setError(message);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [movieId]);

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;
  if (error) return <div className="page"><div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div></div>;
  if (!movie) return <div className="page"><div className="error-message">Movie not found</div></div>;

  const activeShowtimes = showtimes.filter(
    (st) => st.isActive && new Date(st.startTime) > new Date(),
  );

  return (
    <div className="page">
      <div className="container">
        <div className="movie-detail">
          <div className="movie-detail-poster">
            {movie.posterUrl ? (
              <img src={movie.posterUrl} alt={movie.title} />
            ) : (
              <div className="poster-placeholder poster-lg">
                <span>🎬</span>
              </div>
            )}
          </div>

          <div className="movie-detail-info">
            <h1>{movie.title}</h1>
            <div className="movie-meta">
              <span className="badge">{movie.genre}</span>
              <span className="badge">{movie.rating}</span>
              <span>{formatDuration(movie.durationMinutes)}</span>
              <span>Released: {formatDate(movie.releaseDate)}</span>
            </div>
            <p className="movie-description">{movie.description}</p>
          </div>
        </div>

        <section className="section">
          <h2>Available Showtimes</h2>
          {activeShowtimes.length === 0 ? (
            <p className="empty-state">No showtimes available for this movie.</p>
          ) : (
            <div className="showtime-list">
              {activeShowtimes.map((showtime) => (
                <div key={showtime.id} className="showtime-card">
                  <div className="showtime-info">
                    <span className="showtime-datetime">
                      {formatDateTime(showtime.startTime)}
                    </span>
                    <span className="showtime-hall">{showtime.hallName}</span>
                    <span className="showtime-price">
                      From {formatCurrency(showtime.basePrice)}
                    </span>
                    <span className="showtime-seats">
                      {showtime.availableSeats} seats left
                    </span>
                  </div>
                  <Link
                    to={`/showtime/${showtime.id}/seats`}
                    className="btn btn-primary"
                  >
                    Select Seats
                  </Link>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
};
