import React from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Building2, Layers, Film, Clock4, Ticket, Gift, BarChart2 } from 'lucide-react';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';

interface NavCard {
  to: string;
  icon: React.ElementType;
  label: string;
  desc: string;
  color: string;
}

const cards: NavCard[] = [
  { to: '/admin/cinemas', icon: Building2, label: 'Cinemas', desc: 'Manage cinemas, locations and opening hours', color: '#6366f1' },
  { to: '/admin/halls', icon: Layers, label: 'Cinema Halls', desc: 'Manage halls and configure seat layouts', color: '#8b5cf6' },
  { to: '/admin/movies', icon: Film, label: 'Movies', desc: 'Add new titles and update movie details', color: '#a855f7' },
  { to: '/admin/showtimes', icon: Clock4, label: 'Showtimes', desc: 'Schedule and manage movie showtimes', color: '#3b82f6' },
  { to: '/admin/ticket-types', icon: Ticket, label: 'Ticket Types', desc: 'Manage ticket types and price modifiers', color: '#06b6d4' },
  { to: '/admin/loyalty', icon: Gift, label: 'Loyalty Program', desc: 'Configure free ticket reward program', color: '#14b8a6' },
  { to: '/admin/reports', icon: BarChart2, label: 'Reports', desc: 'Ticket sales by date, movie, showtime, and location', color: '#f59e0b' },
];

const container = { hidden: {}, show: { transition: { staggerChildren: 0.07 } } };
const item = { hidden: { opacity: 0, y: 16 }, show: { opacity: 1, y: 0, transition: { duration: 0.25 } } };

export const DashboardPage: React.FC = () => {
  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="md" sx={{ py: 6 }}>
        <Typography variant="h4" fontWeight={700} mb={0.5}>Admin Dashboard</Typography>
        <Typography color="text.secondary" mb={5}>Manage your cinema platform from one place.</Typography>

        <motion.div variants={container} initial="hidden" animate="show">
          <Grid container spacing={2}>
            {cards.map(({ to, icon: Icon, label, desc, color }) => (
              <Grid key={to} size={{ xs: 12, sm: 6, md: 4 }}>
                <motion.div variants={item} style={{ height: '100%' }}>
                  <Paper
                    component={Link}
                    to={to}
                    variant="outlined"
                    sx={{
                      display: 'block', p: 3, height: '100%', textDecoration: 'none',
                      borderRadius: 3, cursor: 'pointer',
                      transition: 'all 0.2s',
                      '&:hover': { boxShadow: 6, transform: 'translateY(-2px)', borderColor: color },
                      '&:hover .card-icon': { bgcolor: `${color}22` },
                      '&:hover .card-title': { color: color },
                    }}
                  >
                    <Box
                      className="card-icon"
                      sx={{
                        mb: 2, width: 44, height: 44, borderRadius: 2,
                        bgcolor: `${color}15`,
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        color, transition: 'bgcolor 0.2s',
                      }}
                    >
                      <Icon size={20} />
                    </Box>
                    <Typography className="card-title" fontWeight={600} mb={0.5} sx={{ transition: 'color 0.2s' }}>
                      {label}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">{desc}</Typography>
                  </Paper>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        </motion.div>
      </Container>
    </Box>
  );
};
