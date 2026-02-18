import React from 'react';
import { Link } from 'react-router-dom';
import type { MovieDto } from '../../types';
import { formatDuration } from '../../utils/formatters';

interface MovieCardProps {
  readonly movie: MovieDto;
  readonly detailPath?: string;
}

export const MovieCard: React.FC<MovieCardProps> = ({ movie, detailPath }) => {
  const linkPath = detailPath ?? `/movies/${movie.id}`;
  return (
    <div className="movie-card">
      <div className="movie-poster">
        {movie.posterUrl ? (
          <img src={movie.posterUrl} alt={movie.title} />
        ) : (
          <div className="poster-placeholder">
            <span>🎬</span>
          </div>
        )}
      </div>
      <div className="movie-info">
        <h3 className="movie-title">{movie.title}</h3>
        <div className="movie-meta">
          <span className="movie-genre">{movie.genre}</span>
          <span className="movie-duration">{formatDuration(movie.durationMinutes)}</span>
          <span className="movie-rating">{movie.rating}</span>
        </div>
        <p className="movie-description">
          {movie.description.length > 120
            ? `${movie.description.slice(0, 120)}...`
            : movie.description}
        </p>
        <Link to={linkPath} className="btn btn-primary">
          View Showtimes
        </Link>
      </div>
    </div>
  );
};
