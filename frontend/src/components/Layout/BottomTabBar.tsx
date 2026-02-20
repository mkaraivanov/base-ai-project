import React, { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import BottomNavigation from '@mui/material/BottomNavigation';
import BottomNavigationAction from '@mui/material/BottomNavigationAction';
import Paper from '@mui/material/Paper';
import Box from '@mui/material/Box';
import Avatar from '@mui/material/Avatar';
import Typography from '@mui/material/Typography';
import MuiButton from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import { Film, Home, LogOut, Ticket } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetClose } from '../UI/sheet';

export const BottomTabBar: React.FC = () => {
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();
  const [accountOpen, setAccountOpen] = useState(false);

  if (!isAuthenticated) return null;

  const handleLogout = () => {
    setAccountOpen(false);
    logout();
    navigate('/login');
  };

  return (
    <>
      <Paper
        elevation={0}
        sx={{
          display: { xs: 'block', md: 'none' },
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          zIndex: 1200,
          borderTop: 1,
          borderColor: 'divider',
        }}
      >
        <BottomNavigation showLabels sx={{ bgcolor: 'background.paper', height: 64 }}>
          <BottomNavigationAction
            label="Cinemas"
            icon={<Home size={20} />}
            component={NavLink}
            to="/"
            end
            sx={{ '&.active': { color: 'primary.main' } }}
          />
          <BottomNavigationAction
            label="Movies"
            icon={<Film size={20} />}
            component={NavLink}
            to="/movies"
            sx={{ '&.active': { color: 'primary.main' } }}
          />
          <BottomNavigationAction
            label="My Tickets"
            icon={<Ticket size={20} />}
            component={NavLink}
            to="/my-bookings"
            sx={{ '&.active': { color: 'primary.main' } }}
          />
          <BottomNavigationAction
            label="Account"
            icon={
              <Avatar sx={{ width: 22, height: 22, bgcolor: 'primary.main', fontSize: 10, fontWeight: 700 }}>
                {user?.firstName?.[0]?.toUpperCase() ?? 'U'}
              </Avatar>
            }
            onClick={() => setAccountOpen(true)}
          />
        </BottomNavigation>
      </Paper>

      {/* Account sheet */}
      <Sheet open={accountOpen} onOpenChange={setAccountOpen} side="bottom">
        <SheetContent>
          <SheetHeader>
            <SheetTitle>Account</SheetTitle>
            <SheetClose onClick={() => setAccountOpen(false)} />
          </SheetHeader>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <Avatar sx={{ width: 48, height: 48, bgcolor: 'primary.main', fontSize: 18, fontWeight: 700 }}>
              {user?.firstName?.[0]?.toUpperCase() ?? 'U'}
            </Avatar>
            <Box>
              <Typography fontWeight={600}>{user?.firstName} {user?.lastName}</Typography>
              <Typography variant="body2" color="text.secondary">{user?.email}</Typography>
            </Box>
          </Box>

          <Divider sx={{ my: 2 }} />

          <MuiButton
            fullWidth
            variant="outlined"
            color="error"
            startIcon={<LogOut size={16} />}
            onClick={handleLogout}
            sx={{ justifyContent: 'flex-start', px: 2, py: 1.5 }}
          >
            Log out
          </MuiButton>
        </SheetContent>
      </Sheet>
    </>
  );
};
