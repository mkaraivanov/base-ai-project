import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Film } from 'lucide-react';
import { motion } from 'framer-motion';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import MuiButton from '@mui/material/Button';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
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
        // empty state handles it
      } finally {
        setLoading(false);
      }
    };
    loadMovies();
  }, []);

  return (
    <Box sx={{ minHeight: '100vh' }}>
      {/* Hero */}
      <Box sx={{
        position: 'relative',
        overflow: 'hidden',
        background: 'linear-gradient(135deg, #1e1b4b 0%, #4c1d95 50%, #09090b 100%)',
        py: { xs: 10, md: 16 },
        px: 2,
      }}>
        <Box sx={{
          position: 'absolute', inset: 0,
          backgroundImage: 'url(https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1400&q=60)',
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          opacity: 0.08,
        }} />

        <Container maxWidth="md" sx={{ position: 'relative', textAlign: 'center' }}>
          <motion.div initial={{ opacity: 0, y: -16 }} animate={{ opacity: 1, y: 0 }}>
            <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 1, bgcolor: 'rgba(99,102,241,0.2)', border: '1px solid rgba(99,102,241,0.3)', borderRadius: 5, px: 2, py: 0.5, mb: 3 }}>
              <Film size={14} color="#a5b4fc" />
              <Typography sx={{ color: '#a5b4fc', fontSize: 13, fontWeight: 500 }}>Your cinema experience starts here</Typography>
            </Box>
          </motion.div>

          <motion.div initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
            <Typography variant="h2" fontWeight={800} color="#fff" lineHeight={1.15} mb={2} sx={{ fontSize: { xs: '2.25rem', md: '3.5rem' } }}>
              Welcome to{' '}
              <Box component="span" sx={{ color: '#818cf8' }}>CineBook</Box>
            </Typography>
          </motion.div>

          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.2 }}>
            <Typography sx={{ color: '#c7d2fe', fontSize: { xs: 16, md: 18 }, mb: 4, maxWidth: 480, mx: 'auto' }}>
              Book your favorite movie tickets in seconds. Browse movies, pick seats, and enjoy the show!
            </Typography>
          </motion.div>

          <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }}>
            <MuiButton
              component={Link}
              to="/movies"
              variant="contained"
              size="large"
              sx={{ px: 4, py: 1.5, fontSize: 16, bgcolor: '#6366f1', '&:hover': { bgcolor: '#4f46e5' } }}
            >
              Browse Movies
            </MuiButton>
          </motion.div>
        </Container>
      </Box>

      {/* Now Showing */}
      <Container maxWidth="lg" sx={{ py: 8 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 4 }}>
          <Typography variant="h5" fontWeight={700}>Now Showing</Typography>
          {movies.length > 0 && (
            <MuiButton component={Link} to="/movies" variant="outlined" size="small">View All</MuiButton>
          )}
        </Box>

        {loading ? (
          <Grid container spacing={2}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Grid key={i} size={{ xs: 6, sm: 4, md: 2 }}>
                <Skeleton variant="rectangular" sx={{ aspectRatio: '2 / 3', borderRadius: 3, mb: 1 }} />
                <Skeleton width="75%" height={18} sx={{ mb: 0.5 }} />
                <Skeleton width="50%" height={14} />
              </Grid>
            ))}
          </Grid>
        ) : movies.length === 0 ? (
          <Typography color="text.secondary" textAlign="center" py={8}>
            No movies available at the moment.
          </Typography>
        ) : (
          <Grid container spacing={2}>
            {movies.map(movie => (
              <Grid key={movie.id} size={{ xs: 6, sm: 4, md: 2 }}>
                <MovieCard movie={movie} />
              </Grid>
            ))}
          </Grid>
        )}
      </Container>
    </Box>
  );
};
