import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { MapPin, Clock, Building2, Search } from 'lucide-react';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import InputAdornment from '@mui/material/InputAdornment';
import TextField from '@mui/material/TextField';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Chip from '@mui/material/Chip';
import { useTranslation } from 'react-i18next';
import { cinemaApi } from '../../api/cinemaApi';
import type { CinemaDto } from '../../types';

const container = { hidden: {}, show: { transition: { staggerChildren: 0.06 } } };
const item = { hidden: { opacity: 0, y: 20 }, show: { opacity: 1, y: 0, transition: { duration: 0.3 } } };

export const CinemaSelectionPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const [cinemas, setCinemas] = useState<readonly CinemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  useEffect(() => {
    const loadCinemas = async () => {
      try {
        const data = await cinemaApi.getAll(true);
        setCinemas(data);
      } catch {
        setError(t('cinemaSelection.error'));
      } finally {
        setLoading(false);
      }
    };
    loadCinemas();
  }, [t]);

  const filtered = cinemas.filter(
    c =>
      c.name.toLowerCase().includes(search.toLowerCase()) ||
      c.city.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <Box sx={{ minHeight: '100vh' }}>
      {/* Hero */}
      <Box sx={{
        position: 'relative', overflow: 'hidden',
        background: 'linear-gradient(135deg, #1e1b4b 0%, #4c1d95 50%, #09090b 100%)',
        py: { xs: 10, md: 14 }, px: 2,
      }}>
        <Box sx={{
          position: 'absolute', inset: 0,
          backgroundImage: 'url(https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1400&q=60)',
          backgroundSize: 'cover', backgroundPosition: 'center', opacity: 0.1,
        }} />
        <Container maxWidth="sm" sx={{ position: 'relative', textAlign: 'center' }}>
          <motion.div initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }}>
            <Typography variant="h3" fontWeight={800} color="#fff" mb={1.5}>
              {t('home.heroTitle')}
            </Typography>
            <Typography sx={{ color: '#c7d2fe', mb: 4, fontSize: 18 }}>
              {t('home.heroDescription')}
            </Typography>
          </motion.div>
          <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }}>
            <TextField
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('movies.searchPlaceholder')}
              size="small"
              fullWidth
              sx={{
                maxWidth: 400, mx: 'auto',
                '& .MuiOutlinedInput-root': {
                  bgcolor: 'rgba(255,255,255,0.1)',
                  '& input': { color: '#fff' },
                  '& input::placeholder': { color: 'rgba(255,255,255,0.5)' },
                  '& fieldset': { borderColor: 'rgba(255,255,255,0.25)' },
                  '&:hover fieldset': { borderColor: 'rgba(255,255,255,0.5)' },
                  '&.Mui-focused fieldset': { borderColor: '#818cf8' },
                },
              }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <Search size={16} color="rgba(255,255,255,0.5)" />
                    </InputAdornment>
                  ),
                },
              }}
            />
          </motion.div>
        </Container>
      </Box>

      {/* Cinema cards */}
      <Container maxWidth="lg" sx={{ py: 6 }}>
        {loading ? (
          <Grid container spacing={3}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Grid key={i} size={{ xs: 12, sm: 6, md: 4 }}>
                <Skeleton variant="rectangular" height={180} sx={{ borderRadius: 3 }} />
              </Grid>
            ))}
          </Grid>
        ) : error ? (
          <Typography color="error" textAlign="center" py={6}>{error}</Typography>
        ) : filtered.length === 0 ? (
          <Typography color="text.secondary" textAlign="center" py={6} className="empty-state">{t('cinemaSelection.noCinemas')}</Typography>
        ) : (
          <motion.div variants={container} initial="hidden" animate="show">
            <Grid container spacing={3}>
              {filtered.map(cinema => (
                <Grid key={cinema.id} size={{ xs: 12, sm: 6, md: 4 }}>
                  <motion.div variants={item} style={{ height: '100%' }}>
                    <Paper
                      className="cinema-card"
                      component={Link}
                      to={`/cinemas/${cinema.id}/movies`}
                      variant="outlined"
                      sx={{
                        display: 'block', p: 3, height: '100%', textDecoration: 'none',
                        borderRadius: 3, transition: 'all 0.2s',
                        '&:hover': { boxShadow: 8, transform: 'translateY(-2px)', borderColor: 'primary.main' },
                      }}
                    >
                      <Box sx={{ display: 'flex', gap: 2, mb: 1.5 }}>
                        <Box sx={{ width: 48, height: 48, borderRadius: 2, bgcolor: 'rgba(99,102,241,0.1)', flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6366f1' }}>
                          {cinema.logoUrl ? (
                            <Box component="img" src={cinema.logoUrl} alt={cinema.name} sx={{ width: 32, height: 32, objectFit: 'contain' }} />
                          ) : (
                            <Building2 size={22} />
                          )}
                        </Box>
                        <Box sx={{ flex: 1, minWidth: 0 }}>
                          <Typography fontWeight={600} noWrap>{cinema.name}</Typography>
                          <Typography className="cinema-location" variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <MapPin size={11} />
                            {cinema.city}, {cinema.country}
                          </Typography>
                        </Box>
                      </Box>

                      <Typography variant="body2" color="text.secondary" noWrap mb={2}>{cinema.address}</Typography>

                      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <Typography className="cinema-hours" variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Clock size={11} />
                          {cinema.openTime} – {cinema.closeTime}
                        </Typography>
                        <Chip label={t('cinemaSelection.halls', { count: cinema.hallCount })} size="small" variant="outlined" />
                      </Box>
                    </Paper>
                  </motion.div>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}
      </Container>
    </Box>
  );
};
