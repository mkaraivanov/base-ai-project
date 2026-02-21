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
import Grid from '@mui/material/Grid';
import CircularProgress from '@mui/material/CircularProgress';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import type { RegisterDto } from '../../types';
import { extractErrorMessage } from '../../utils/errorHandler';

const POSTER = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&q=80';

export const RegisterPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const [form, setForm] = useState({
    firstName: '', lastName: '', email: '',
    password: '', confirmPassword: '', phoneNumber: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.password !== form.confirmPassword) { toast.error(t('register.passwordMismatch')); return; }
    if (form.password.length < 6) { toast.error(t('register.passwordTooShort')); return; }
    try {
      setLoading(true);
      const registerData: RegisterDto = {
        firstName: form.firstName, lastName: form.lastName,
        email: form.email, password: form.password, phoneNumber: form.phoneNumber,
      };
      await register(registerData);
      navigate('/');
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err, t('register.failed')));
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
              Join the experience.
            </Typography>
            <Typography sx={{ color: '#c7d2fe' }}>
              Create your free account and start booking in seconds.
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Right panel — form */}
      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3, bgcolor: 'background.default', overflowY: 'auto' }}>
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, ease: 'easeOut' }}
          style={{ width: '100%', maxWidth: 360, paddingTop: 32, paddingBottom: 32 }}
        >
          <Box component={Link} to="/" sx={{ display: { xs: 'flex', md: 'none' }, alignItems: 'center', gap: 1, textDecoration: 'none', color: 'inherit', fontWeight: 700, fontSize: 20, mb: 4 }}>
            <Box sx={{ width: 36, height: 36, borderRadius: 2, bgcolor: 'primary.main', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Film size={18} color="#fff" />
            </Box>
            CineBook
          </Box>

          <Typography variant="h5" fontWeight={700} mb={0.5}>{t('register.title')}</Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            {t('register.subtitle')}
          </Typography>

          <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Grid container spacing={1.5}>
              <Grid size={6}>
                <TextField label={t('register.firstName')} name="firstName" value={form.firstName} onChange={handleChange} required autoComplete="given-name" fullWidth size="small" />
              </Grid>
              <Grid size={6}>
                <TextField label={t('register.lastName')} name="lastName" value={form.lastName} onChange={handleChange} required autoComplete="family-name" fullWidth size="small" />
              </Grid>
            </Grid>

            <TextField label={t('register.email')} name="email" type="email" value={form.email} onChange={handleChange} placeholder={t('register.emailPlaceholder')} required autoComplete="email" fullWidth size="small" />

            <TextField label={t('register.phone')} name="phoneNumber" type="tel" value={form.phoneNumber} onChange={handleChange} placeholder={t('register.phonePlaceholder')} required autoComplete="tel" fullWidth size="small" />

            <TextField
              label={t('register.password')}
              name="password"
              type={showPassword ? 'text' : 'password'}
              value={form.password}
              onChange={handleChange}
              placeholder={t('register.passwordPlaceholder')}
              required
              autoComplete="new-password"
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

            <TextField
              label={t('register.confirmPassword')}
              name="confirmPassword"
              type={showConfirm ? 'text' : 'password'}
              value={form.confirmPassword}
              onChange={handleChange}
              placeholder={t('register.passwordPlaceholder')}
              required
              autoComplete="new-password"
              fullWidth
              size="small"
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton size="small" onClick={() => setShowConfirm(v => !v)} tabIndex={-1}>
                        {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />

            <MuiButton type="submit" variant="contained" fullWidth disabled={loading} sx={{ mt: 1, py: 1.25 }}>
              {loading ? <CircularProgress size={20} color="inherit" /> : t('register.submit')}
            </MuiButton>
          </Box>

          <Typography variant="body2" color="text.secondary" textAlign="center" mt={3}>
            {t('register.hasAccount')}{' '}
            <Box component={Link} to="/login" sx={{ color: 'primary.main', fontWeight: 500, '&:hover': { textDecoration: 'underline' } }}>
              {t('register.loginLink')}
            </Box>
          </Typography>
        </motion.div>
      </Box>
    </Box>
  );
};
