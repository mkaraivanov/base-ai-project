import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { movieApi } from '../../api/movieApi';
import { MovieCard } from '../../components/MovieCard/MovieCard';
import type { MovieDto } from '../../types';

export const HomePage: React.FC = () => {
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadMovies = async () => {
      try {
        const data = await movieApi.getAll(true);
        setMovies(data.slice(0, 6));
      } catch {
        console.error('Failed to load movies');
      } finally {
        setLoading(false);
      }
    };
    loadMovies();
  }, []);

  return (
    <div className="page home-page">
      <section className="hero">
        <div className="hero-content">
          <h1>Welcome to CineBook</h1>
          <p>Book your favorite movie tickets in seconds. Browse movies, pick seats, and enjoy the show!</p>
          <Link to="/movies" className="btn btn-primary btn-lg">
            Browse Movies
          </Link>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <h2>Now Showing</h2>
          {loading ? (
            <div className="loading">Loading movies...</div>
          ) : movies.length === 0 ? (
            <p className="empty-state">No movies available at the moment.</p>
          ) : (
            <div className="movie-grid">
              {movies.map((movie) => (
                <MovieCard key={movie.id} movie={movie} />
              ))}
            </div>
          )}
          {movies.length > 0 && (
            <div className="section-footer">
              <Link to="/movies" className="btn btn-outline">
                View All Movies
              </Link>
            </div>
          )}
        </div>
      </section>
    </div>
  );
};
