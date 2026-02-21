import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Search } from 'lucide-react';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import InputAdornment from '@mui/material/InputAdornment';
import TextField from '@mui/material/TextField';
import Chip from '@mui/material/Chip';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
import Paper from '@mui/material/Paper';
import { movieApi } from '../../api/movieApi';
import { MovieCard } from '../../components/MovieCard/MovieCard';
import type { MovieDto } from '../../types';

export const MoviesPage: React.FC = () => {
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
        // silent – empty state handles it
      } finally {
        setLoading(false);
      }
    };
    loadMovies();
  }, []);

  const genres = ['', ...new Set(movies.map(m => m.genre))].sort();

  const filteredMovies = movies.filter(movie => {
    const matchesSearch =
      movie.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
      movie.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesGenre = genreFilter === '' || movie.genre === genreFilter;
    return matchesSearch && matchesGenre;
  });

  return (
    <Box sx={{ minHeight: '100vh' }}>
      {/* Sticky header strip */}
      <Paper
        variant="outlined"
        square
        elevation={0}
        sx={{
          position: 'sticky', top: 64, zIndex: 30,
          borderTop: 'none', borderLeft: 'none', borderRight: 'none',
          py: 1.5, px: 2,
        }}
      >
        <Container maxWidth="lg" disableGutters>
          <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 2, alignItems: { sm: 'center' } }}>
            <Typography variant="h6" fontWeight={700} flexShrink={0}>Now Showing</Typography>
            <Box sx={{ flex: 1, display: 'flex', gap: 1.5, flexWrap: 'wrap', alignItems: 'center' }}>
              <TextField
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                placeholder="Search movies…"
                size="small"
                sx={{ minWidth: 200, flex: 1 }}
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <Search size={14} />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <Box sx={{ display: 'flex', gap: 0.75, flexWrap: 'nowrap', overflowX: 'auto', pb: 0.25 }}>
                {genres.map(g => (
                  <Chip
                    key={g}
                    label={g === '' ? 'All' : g}
                    size="small"
                    onClick={() => setGenreFilter(g)}
                    variant={genreFilter === g ? 'filled' : 'outlined'}
                    color={genreFilter === g ? 'primary' : 'default'}
                    sx={{ flexShrink: 0, cursor: 'pointer' }}
                  />
                ))}
              </Box>
            </Box>
          </Box>
        </Container>
      </Paper>

      <Container maxWidth="lg" sx={{ py: 4 }}>
        {loading ? (
          <Grid container spacing={2}>
            {Array.from({ length: 10 }).map((_, i) => (
              <Grid key={i} size={{ xs: 6, sm: 4, md: 3, lg: 2.4 }}>
                <Skeleton variant="rectangular" sx={{ aspectRatio: '2 / 3', borderRadius: 3, mb: 1 }} />
                <Skeleton width="75%" height={18} sx={{ mb: 0.5 }} />
                <Skeleton width="50%" height={14} />
              </Grid>
            ))}
          </Grid>
        ) : filteredMovies.length === 0 ? (
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
            <Typography color="text.secondary" textAlign="center" py={10}>
              No movies match your search.
            </Typography>
          </motion.div>
        ) : (
          <Grid container spacing={2}>
            {filteredMovies.map(movie => (
              <Grid key={movie.id} size={{ xs: 6, sm: 4, md: 3, lg: 2.4 }}>
                <MovieCard movie={movie} />
              </Grid>
            ))}
          </Grid>
        )}
      </Container>
    </Box>
  );
};
