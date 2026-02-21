import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Eye, EyeOff, Film } from 'lucide-react';
import { toast } from 'sonner';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import TextField from '@mui/material/TextField';
import MuiButton from '@mui/material/Button';
import InputAdornment from '@mui/material/InputAdornment';
import IconButton from '@mui/material/IconButton';
import CircularProgress from '@mui/material/CircularProgress';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { extractErrorMessage } from '../../utils/errorHandler';

const POSTER = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&q=80';

export const LoginPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) { toast.error(t('login.fillAllFields')); return; }
    try {
      setLoading(true);
      await login(email, password);
      navigate('/');
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err, t('login.error')));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* Left panel — cinematic backdrop */}
      <Box sx={{ display: { xs: 'none', md: 'flex' }, width: '50%', position: 'relative', overflow: 'hidden' }}>
        <Box component="img" src={POSTER} alt="Cinema" sx={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover' }} />
        <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(135deg, rgba(30,27,75,0.9) 0%, rgba(88,28,135,0.8) 50%, rgba(0,0,0,0.7) 100%)' }} />
        <Box sx={{ position: 'relative', zIndex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'space-between', p: 5, width: '100%' }}>
          <Box component={Link} to="/" sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#fff', textDecoration: 'none', fontWeight: 700, fontSize: 20 }}>
            <Box sx={{ width: 36, height: 36, borderRadius: 2, bgcolor: '#6366f1', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Film size={18} color="#fff" />
            </Box>
            CineBook
          </Box>
          <Box>
            <Typography variant="h4" fontWeight={700} color="#fff" lineHeight={1.2} mb={1.5}>
              Your cinema,<br />your way.
            </Typography>
            <Typography sx={{ color: '#c7d2fe' }}>
              Book the best seats, discover new films, earn loyalty rewards.
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Right panel — form */}
      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3, bgcolor: 'background.default' }}>
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, ease: 'easeOut' }}
          style={{ width: '100%', maxWidth: 360 }}
        >
          <Box component={Link} to="/" sx={{ display: { xs: 'flex', md: 'none' }, alignItems: 'center', gap: 1, textDecoration: 'none', color: 'inherit', fontWeight: 700, fontSize: 20, mb: 4 }}>
            <Box sx={{ width: 36, height: 36, borderRadius: 2, bgcolor: 'primary.main', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Film size={18} color="#fff" />
            </Box>
            CineBook
          </Box>

          <Typography variant="h5" fontWeight={700} mb={0.5}>{t('login.title')}</Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            {t('login.subtitle')}
          </Typography>

          <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              id="email"
              label={t('login.email')}
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder={t('login.emailPlaceholder')}
              required
              autoComplete="email"
              fullWidth
              size="small"
            />

            <TextField
              id="password"
              label={t('login.password')}
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              autoComplete="current-password"
              fullWidth
              size="small"
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton size="small" onClick={() => setShowPassword(v => !v)} tabIndex={-1}>
                        {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />

            <MuiButton
              type="submit"
              variant="contained"
              fullWidth
              disabled={loading}
              sx={{ mt: 1, py: 1.25 }}
            >
              {loading ? <CircularProgress size={20} color="inherit" /> : t('login.submit')}
            </MuiButton>
          </Box>

          <Typography variant="body2" color="text.secondary" textAlign="center" mt={3}>
            {t('login.noAccount')}{' '}
            <Box component={Link} to="/register" sx={{ color: 'primary.main', fontWeight: 500, '&:hover': { textDecoration: 'underline' } }}>
              {t('login.signUpLink')}
            </Box>
          </Typography>
        </motion.div>
      </Box>
    </Box>
  );
};
