import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { MapPin, Clock, Building2, ChevronLeft } from 'lucide-react';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
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
        const movieIdsWithShowtimes = new Set(showtimes.map(s => s.movieId));
        setMovies(allMovies.filter(m => movieIdsWithShowtimes.has(m.id)));
      } catch {
        setError(t('cinemaMovies.error'));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [cinemaId, t]);

  return (
    <Box sx={{ minHeight: '100vh' }}>
      {/* Cinema header */}
      <Paper variant="outlined" square elevation={0} sx={{ borderTop: 'none', borderLeft: 'none', borderRight: 'none' }}>
        <Container maxWidth="lg" sx={{ py: 2.5 }}>
          <Box className="page-breadcrumb">
            <MuiButton
              component={Link}
              to="/"
              startIcon={<ChevronLeft size={14} />}
              size="small"
              color="inherit"
              sx={{ mb: 2, color: 'text.secondary', '&:hover': { color: 'primary.main' } }}
            >
              {t('cinemaMovies.allCinemas')}
            </MuiButton>
          </Box>

          {cinema && (
            <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Box sx={{ width: 56, height: 56, flexShrink: 0, borderRadius: 2, bgcolor: 'rgba(99,102,241,0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6366f1' }}>
                  {cinema.logoUrl ? (
                    <Box component="img" src={cinema.logoUrl} alt={cinema.name} sx={{ width: 40, height: 40, objectFit: 'contain' }} />
                  ) : (
                    <Building2 size={26} />
                  )}
                </Box>
                <Box>
                  <Typography variant="h6" fontWeight={700}>{cinema.name}</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                    <Typography className="cinema-location" variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <MapPin size={12} />{cinema.address}, {cinema.city}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Clock size={12} />{cinema.openTime} – {cinema.closeTime}
                    </Typography>
                  </Box>
                </Box>
              </Box>
            </motion.div>
          )}
        </Container>
      </Paper>

      {/* Movies */}
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography variant="h6" component="h2" fontWeight={600} mb={3}>{t('cinemaMovies.nowShowing')}</Typography>

        {loading ? (
          <Grid container spacing={2}>
            {Array.from({ length: 8 }).map((_, i) => (
              <Grid key={i} size={{ xs: 6, sm: 4, md: 3, lg: 2.4 }}>
                <Skeleton variant="rectangular" sx={{ aspectRatio: '2 / 3', borderRadius: 3, mb: 1 }} />
                <Skeleton width="75%" height={18} sx={{ mb: 0.5 }} />
                <Skeleton width="50%" height={14} />
              </Grid>
            ))}
          </Grid>
        ) : error ? (
          <Typography className="empty-state" color="error">{error}</Typography>
        ) : movies.length === 0 ? (
          <Typography className="empty-state" color="text.secondary">{t('cinemaMovies.noMovies')}</Typography>
        ) : (
          <Grid container spacing={2}>
            {movies.map(movie => (
              <Grid key={movie.id} size={{ xs: 6, sm: 4, md: 3, lg: 2.4 }}>
                <MovieCard
                  movie={movie}
                  detailPath={`/cinemas/${cinemaId}/movies/${movie.id}`}
                />
              </Grid>
            ))}
          </Grid>
        )}
      </Container>
    </Box>
  );
};
