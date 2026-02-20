import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Clock, Calendar, Star, Film, ChevronLeft, Ticket } from 'lucide-react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import MuiButton from '@mui/material/Button';
import Paper from '@mui/material/Paper';
import type { MovieDto, ShowtimeDto } from '../../types';
import { formatDate, formatDateTime, formatDuration, formatCurrency } from '../../utils/formatters';
import { Badge } from '../UI/badge';

interface MovieDetailLayoutProps {
  readonly movie: MovieDto;
  readonly showtimes: readonly ShowtimeDto[];
  readonly backTo: string;
  readonly backLabel: string;
  readonly cinemaName?: string;
}

export const MovieDetailLayout: React.FC<MovieDetailLayoutProps> = ({
  movie, showtimes, backTo, backLabel, cinemaName,
}) => {
  const activeShowtimes = showtimes.filter(
    st => st.isActive && new Date(st.startTime) > new Date(),
  );
  const [posterError, setPosterError] = useState(false);
  const showPoster = !!movie.posterUrl && !posterError;

  const grouped = activeShowtimes.reduce<Record<string, ShowtimeDto[]>>((acc, st) => {
    const date = formatDate(st.startTime);
    return { ...acc, [date]: [...(acc[date] ?? []), st] };
  }, {});

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      {/* Backdrop hero */}
      <Box sx={{ position: 'relative', height: { xs: 320, md: 420 }, overflow: 'hidden' }}>
        {showPoster ? (
          <Box
            component="img"
            src={movie.posterUrl}
            alt={movie.title}
            onError={() => setPosterError(true)}
            sx={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', transform: 'scale(1.1)', filter: 'blur(6px)', opacity: 0.4 }}
          />
        ) : (
          <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(135deg, #1e1b4b 0%, #18181b 100%)' }} />
        )}
        <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(to top, var(--mui-palette-background-default) 0%, rgba(0,0,0,0.6) 60%, rgba(0,0,0,0.3) 100%)' }} />

        <Box sx={{ position: 'relative', zIndex: 1, height: '100%', maxWidth: 1152, mx: 'auto', px: 2, display: 'flex', flexDirection: 'column', justifyContent: 'flex-end', pb: 3 }}>
          <Box
            component={Link}
            to={backTo}
            sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, color: 'rgba(255,255,255,0.7)', textDecoration: 'none', fontSize: 14, mb: 2, alignSelf: 'flex-start', '&:hover': { color: '#fff' } }}
          >
            <ChevronLeft size={14} />
            {backLabel}
          </Box>

          <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}>
            <Box sx={{ display: 'flex', gap: 2.5, alignItems: 'flex-end' }}>
              {/* Poster thumbnail */}
              <Box sx={{ display: { xs: 'none', sm: 'block' }, flexShrink: 0, width: 112, aspectRatio: '2/3', borderRadius: 2, overflow: 'hidden', boxShadow: 8, border: '1px solid rgba(255,255,255,0.1)' }}>
                {showPoster ? (
                  <Box component="img" src={movie.posterUrl} alt={movie.title} onError={() => setPosterError(true)} sx={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                ) : (
                  <Box sx={{ width: '100%', height: '100%', bgcolor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <Film size={28} color="rgba(255,255,255,0.4)" />
                  </Box>
                )}
              </Box>

              {/* Info */}
              <Box>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
                  <Badge variant="secondary">{movie.genre}</Badge>
                  {movie.rating && (
                    <Badge variant="outline" sx={{ color: '#fbbf24', borderColor: 'rgba(251,191,36,0.5)' }}>
                      <Star size={10} fill="#fbbf24" style={{ marginRight: 4 }} /> {movie.rating}
                    </Badge>
                  )}
                </Box>
                <Typography variant="h4" fontWeight={700} color="#fff" sx={{ fontSize: { xs: '1.5rem', md: '2.25rem' } }}>
                  {movie.title}
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, mt: 1, color: 'rgba(255,255,255,0.7)', fontSize: 14 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <Clock size={13} /> {formatDuration(movie.durationMinutes)}
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <Calendar size={13} /> {formatDate(movie.releaseDate)}
                  </Box>
                  {cinemaName && (
                    <Typography component="span" sx={{ color: '#a5b4fc', fontWeight: 500, fontSize: 14 }}>
                      @ {cinemaName}
                    </Typography>
                  )}
                </Box>
              </Box>
            </Box>
          </motion.div>
        </Box>
      </Box>

      {/* Body */}
      <Box sx={{ maxWidth: 1152, mx: 'auto', px: 2, py: 4, display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 300px' }, gap: 4 }}>
        {/* Description */}
        <Box>
          <Typography variant="h6" fontWeight={600} mb={1.5}>About</Typography>
          <Typography color="text.secondary" sx={{ lineHeight: 1.7 }}>{movie.description}</Typography>
        </Box>

        {/* Showtimes */}
        <Box>
          <Typography variant="h6" fontWeight={600} mb={1.5}>
            {cinemaName ? `Showtimes at ${cinemaName}` : 'Available Showtimes'}
          </Typography>

          {activeShowtimes.length === 0 ? (
            <Typography variant="body2" color="text.secondary">No upcoming showtimes available.</Typography>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
              {Object.entries(grouped).map(([date, times]) => (
                <Box key={date}>
                  <Typography variant="caption" fontWeight={600} textTransform="uppercase" letterSpacing={1} color="text.secondary" display="block" mb={1}>
                    {date}
                  </Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {times.map(showtime => (
                      <Paper
                        key={showtime.id}
                        variant="outlined"
                        sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', px: 2, py: 1.5, borderRadius: 2 }}
                      >
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {formatDateTime(showtime.startTime).split(',')[1]?.trim() ?? formatDateTime(showtime.startTime)}
                          </Typography>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.25 }}>
                            <Typography variant="caption" color="text.secondary">{showtime.hallName}</Typography>
                            <Typography variant="caption" color="text.disabled">·</Typography>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <Ticket size={10} />
                              <Typography variant="caption" color="text.secondary">{showtime.availableSeats} left</Typography>
                            </Box>
                          </Box>
                        </Box>
                        <Box sx={{ textAlign: 'right', ml: 1.5 }}>
                          <Typography variant="caption" color="text.secondary" display="block" mb={0.5}>
                            from {formatCurrency(showtime.basePrice)}
                          </Typography>
                          <MuiButton
                            component={Link}
                            to={`/showtime/${showtime.id}/seats`}
                            variant="contained"
                            size="small"
                            sx={{ minWidth: 64, fontSize: 12 }}
                          >
                            Book
                          </MuiButton>
                        </Box>
                      </Paper>
                    ))}
                  </Box>
                </Box>
              ))}
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
};
