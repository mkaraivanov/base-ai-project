import React, { useState } from 'react';
import { Link, NavLink, useNavigate, useLocation } from 'react-router-dom';
import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Typography from '@mui/material/Typography';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Divider from '@mui/material/Divider';
import Avatar from '@mui/material/Avatar';
import MuiButton from '@mui/material/Button';
import { ChevronLeft, Film, LayoutDashboard, LogOut, Ticket } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { ThemeToggle } from '../UI/ThemeToggle';
import { LanguageSwitcher } from '../LanguageSwitcher/LanguageSwitcher';

export const Navbar: React.FC = () => {
  const { t } = useTranslation('common');
  const { isAuthenticated, isAdmin, user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const ROOT_PATHS = new Set(['/', '/movies', '/my-bookings', '/admin', '/login', '/register']);
  const showBackButton = !ROOT_PATHS.has(location.pathname);

  const handleMenuOpen = (e: React.MouseEvent<HTMLElement>) => setAnchorEl(e.currentTarget);
  const handleMenuClose = () => setAnchorEl(null);

  const handleLogout = () => {
    handleMenuClose();
    logout();
    navigate('/login');
  };

  const initials = user?.firstName?.[0]?.toUpperCase() ?? 'U';

  return (
    <AppBar
      position="sticky"
      color="default"
      elevation={0}
      sx={{
        borderBottom: 1,
        borderColor: 'divider',
        backdropFilter: 'blur(12px)',
        bgcolor: 'background.paper',
      }}
    >
      <Toolbar sx={{ maxWidth: 1280, width: '100%', mx: 'auto', gap: 2, px: { xs: 2, sm: 3 } }}>
        {/* Back button */}
        {showBackButton && (
          <IconButton
            onClick={() => navigate(-1)}
            size="small"
            aria-label={t('actions.back')}
            sx={{ flexShrink: 0 }}
          >
            <ChevronLeft size={20} />
          </IconButton>
        )}

        {/* Brand */}
        <Box component={Link} to="/" sx={{ display: 'flex', alignItems: 'center', gap: 1, textDecoration: 'none', color: 'inherit', flexShrink: 0 }}>
          <Box sx={{ width: 32, height: 32, borderRadius: 1, bgcolor: 'primary.main', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff' }}>
            <Film size={16} />
          </Box>
          <Typography fontWeight={700} fontSize={18} sx={{ display: { xs: 'none', sm: 'block' } }}>
            CineBook
          </Typography>
        </Box>

        {/* Desktop nav links */}
        {isAuthenticated && (
          <Box sx={{ display: { xs: 'none', md: 'flex' }, alignItems: 'center', gap: 0.5, flex: 1, justifyContent: 'center' }}>
            {[
              { to: '/movies',      icon: <Film size={15} />,           label: t('nav.movies')      },
              { to: '/my-bookings', icon: <Ticket size={15} />,         label: t('nav.myTickets')   },
              ...(isAdmin ? [{ to: '/admin', icon: <LayoutDashboard size={15} />, label: t('nav.admin') }] : []),
            ].map(({ to, icon, label }) => (
              <NavLink key={to} to={to} style={{ textDecoration: 'none' }}>
                {({ isActive }) => (
                  <MuiButton
                    variant={isActive ? 'contained' : 'text'}
                    color={isActive ? 'primary' : 'inherit'}
                    size="small"
                    startIcon={icon}
                    sx={{ borderRadius: 2, textTransform: 'none', fontWeight: 500 }}
                  >
                    {label}
                  </MuiButton>
                )}
              </NavLink>
            ))}
          </Box>
        )}

        <Box sx={{ flex: 1 }} />

        {/* Right actions */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexShrink: 0 }}>
          <LanguageSwitcher />
          <ThemeToggle />

          {isAuthenticated ? (
            <>
              <IconButton onClick={handleMenuOpen} size="small" aria-label="Open user menu">
                <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 13, fontWeight: 700 }}>
                  {initials}
                </Avatar>
              </IconButton>

              <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleMenuClose}
                transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                slotProps={{ paper: { elevation: 4, sx: { mt: 1, minWidth: 220, borderRadius: 2 } } }}
              >
                {/* User header */}
                <Box sx={{ px: 2, py: 1.5 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontSize: 14, fontWeight: 700 }}>{initials}</Avatar>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2" fontWeight={600} noWrap>{user?.firstName} {user?.lastName}</Typography>
                      <Typography variant="caption" color="text.secondary" noWrap>{user?.email}</Typography>
                    </Box>
                  </Box>
                </Box>
                <Divider />
                <MenuItem component={Link} to="/my-bookings" onClick={handleMenuClose} sx={{ gap: 1.5, py: 1.2, fontSize: 14 }}>
                  <Ticket size={15} />
                  {t('nav.bookings')}
                </MenuItem>
                {isAdmin && (
                  <MenuItem component={Link} to="/admin" onClick={handleMenuClose} sx={{ gap: 1.5, py: 1.2, fontSize: 14 }}>
                    <LayoutDashboard size={15} />
                    Admin Panel
                  </MenuItem>
                )}
                <Divider />
                <MenuItem onClick={handleLogout} sx={{ gap: 1.5, py: 1.2, fontSize: 14, color: 'error.main' }}>
                  <LogOut size={15} />
                  {t('nav.logout')}
                </MenuItem>
              </Menu>
            </>
          ) : (
            <Box sx={{ display: 'flex', gap: 1 }}>
              <MuiButton component={Link} to="/login" variant="outlined" size="small">{t('nav.login')}</MuiButton>
              <MuiButton component={Link} to="/register" variant="contained" size="small" sx={{ display: { xs: 'none', sm: 'flex' } }}>{t('nav.signUp')}</MuiButton>
            </Box>
          )}
        </Box>
      </Toolbar>
    </AppBar>
  );
};
