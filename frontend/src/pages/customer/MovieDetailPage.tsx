import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Skeleton from '@mui/material/Skeleton';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import { useTranslation } from 'react-i18next';
import { movieApi } from '../../api/movieApi';
import { showtimeApi } from '../../api/showtimeApi';
import type { MovieDto, ShowtimeDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';
import { MovieDetailLayout } from '../../components/MovieDetail/MovieDetailLayout';

export const MovieDetailPage: React.FC = () => {
  const { t } = useTranslation('customer');
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
        setError(extractErrorMessage(err, t('movieDetail.loadFailed')));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [movieId]);

  if (loading) return (
    <Box sx={{ minHeight: '100vh', p: 4 }}>
      <Skeleton variant="rectangular" sx={{ height: 360, borderRadius: 3, mb: 4 }} />
      <Container maxWidth="lg">
        <Grid container spacing={4}>
          <Grid size={{ xs: 12, md: 8 }}>
            <Skeleton height={20} sx={{ mb: 1 }} />
            <Skeleton width="80%" height={20} sx={{ mb: 1 }} />
            <Skeleton width="60%" height={20} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} height={64} sx={{ borderRadius: 2, mb: 1 }} />
            ))}
          </Grid>
        </Grid>
      </Container>
    </Box>
  );

  if (error) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="error" sx={{ whiteSpace: 'pre-line' }}>{error}</Typography>
    </Box>
  );

  if (!movie) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="text.secondary">{t('movieDetail.notFound')}</Typography>
    </Box>
  );

  return (
    <MovieDetailLayout
      movie={movie}
      showtimes={showtimes}
      backTo="/movies"
      backLabel="All Movies"
    />
  );
};
