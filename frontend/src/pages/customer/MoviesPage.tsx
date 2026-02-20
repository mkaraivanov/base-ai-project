import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { movieApi } from '../../api/movieApi';
import { MovieCard } from '../../components/MovieCard/MovieCard';
import type { MovieDto } from '../../types';

export const MoviesPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const [movies, setMovies] = useState<readonly MovieDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [genreFilter, setGenreFilter] = useState('');

  useEffect(() => {
    const loadMovies = async () => {
      try {
        const data = await movieApi.getAll(true);
        setMovies(data);
      } catch {
        console.error('Failed to load movies');
      } finally {
        setLoading(false);
      }
    };
    loadMovies();
  }, []);

  const genres = [...new Set(movies.map((m) => m.genre))].sort();

  const filteredMovies = movies.filter((movie) => {
    const matchesSearch =
      movie.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
      movie.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesGenre = genreFilter === '' || movie.genre === genreFilter;
    return matchesSearch && matchesGenre;
  });

  if (loading) {
    return <div className="page"><div className="loading">{t('movies.loading')}</div></div>;
  }

  return (
    <div className="page">
      <div className="container">
        <h1>{t('movies.title')}</h1>

        <div className="filters">
          <input
            type="text"
            placeholder={t('movies.searchPlaceholder')}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="input"
          />
          <select
            value={genreFilter}
            onChange={(e) => setGenreFilter(e.target.value)}
            className="input"
          >
            <option value="">{t('movies.allGenres')}</option>
            {genres.map((genre) => (
              <option key={genre} value={genre}>
                {genre}
              </option>
            ))}
          </select>
        </div>

        {filteredMovies.length === 0 ? (
          <p className="empty-state">{t('movies.noResults')}</p>
        ) : (
          <div className="movie-grid">
            {filteredMovies.map((movie) => (
              <MovieCard key={movie.id} movie={movie} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
