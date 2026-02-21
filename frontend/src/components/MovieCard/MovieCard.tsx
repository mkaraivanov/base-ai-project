import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Clock, Star, Film } from 'lucide-react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import MuiButton from '@mui/material/Button';
import type { MovieDto } from '../../types';
import { formatDuration } from '../../utils/formatters';
import { Badge } from '../UI/badge';

interface MovieCardProps {
  readonly movie: MovieDto;
  readonly detailPath?: string;
}

export const MovieCard: React.FC<MovieCardProps> = ({ movie, detailPath }) => {
  const linkPath = detailPath ?? `/movies/${movie.id}`;
  const [posterError, setPosterError] = useState(false);
  const showPoster = !!movie.posterUrl && !posterError;

  return (
    <motion.div
      whileHover={{ y: -4 }}
      transition={{ duration: 0.2 }}
      style={{ height: '100%' }}
    >
      <Box
        className="movie-card"
        sx={{
          borderRadius: 3,
          overflow: 'hidden',
          border: 1,
          borderColor: 'divider',
          bgcolor: 'background.paper',
          boxShadow: 1,
          transition: 'box-shadow 0.2s',
          '&:hover': { boxShadow: 6 },
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* Poster */}
        <Box
          component={Link}
          to={linkPath}
          sx={{
            display: 'block',
            aspectRatio: '2/3',
            overflow: 'hidden',
            bgcolor: 'background.default',
            position: 'relative',
            textDecoration: 'none',
            '&:hover .hover-overlay': { opacity: 1 },
          }}
        >
          {showPoster ? (
            <Box
              component="img"
              src={movie.posterUrl}
              alt={movie.title}
              onError={() => setPosterError(true)}
              sx={{ width: '100%', height: '100%', objectFit: 'cover', transition: 'transform 0.5s', '&:hover': { transform: 'scale(1.05)' } }}
            />
          ) : (
            <Box sx={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 1, borderBottom: 1, borderColor: 'divider', borderStyle: 'dashed' }}>
              <Film size={36} color="var(--mui-palette-text-disabled)" />
              <Typography variant="caption" color="text.disabled">No poster</Typography>
            </Box>
          )}
          {/* Hover overlay */}
          <Box
            className="hover-overlay"
            sx={{
              position: 'absolute', inset: 0,
              background: 'linear-gradient(to top, rgba(0,0,0,0.8) 0%, rgba(0,0,0,0.2) 60%, transparent 100%)',
              opacity: 0, transition: 'opacity 0.3s',
              display: 'flex', alignItems: 'flex-end', p: 2,
            }}
          >
            <Typography variant="body2" fontWeight={600} color="#fff">View Showtimes →</Typography>
          </Box>
        </Box>

        {/* Info */}
        <Box sx={{ p: 2, flex: 1, display: 'flex', flexDirection: 'column' }}>
          <Box component={Link} to={linkPath} sx={{ textDecoration: 'none', color: 'inherit', '&:hover': { color: 'primary.main' }, mb: 1 }}>
            <Typography fontWeight={600} noWrap sx={{ transition: 'color 0.15s' }}>
              {movie.title}
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 0.75, mb: 1.5 }}>
            <Badge variant="secondary">{movie.genre}</Badge>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <Clock size={11} />
              <Typography variant="caption" color="text.secondary">{formatDuration(movie.durationMinutes)}</Typography>
            </Box>
            {movie.rating && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: '#f59e0b' }}>
                <Star size={11} fill="#f59e0b" />
                <Typography variant="caption" sx={{ color: '#f59e0b' }}>{movie.rating}</Typography>
              </Box>
            )}
          </Box>

          <Typography variant="caption" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', mb: 2, flex: 1 }}>
            {movie.description}
          </Typography>

          <MuiButton
            className="btn"
            component={Link}
            to={linkPath}
            variant="contained"
            size="small"
            fullWidth
            sx={{ mt: 'auto' }}
          >
            Book Now
          </MuiButton>
        </Box>
      </Box>
    </motion.div>
  );
};
