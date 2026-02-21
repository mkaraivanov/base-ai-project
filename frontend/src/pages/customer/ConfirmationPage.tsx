import React, { useEffect, useState, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { CheckCircle2, Calendar, MapPin, Armchair, Receipt, Car, Ticket } from 'lucide-react';
import confetti from 'canvas-confetti';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Divider from '@mui/material/Divider';
import { bookingApi } from '../../api/bookingApi';
import type { BookingDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

export const ConfirmationPage: React.FC = () => {
  const { bookingNumber } = useParams<{ bookingNumber: string }>();
  const [booking, setBooking] = useState<BookingDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const firedRef = useRef(false);

  useEffect(() => {
    const loadBooking = async () => {
      if (!bookingNumber) return;
      try {
        const data = await bookingApi.getByBookingNumber(bookingNumber);
        setBooking(data);
      } catch (err: unknown) {
        setError(extractErrorMessage(err, 'Failed to load booking details'));
      } finally {
        setLoading(false);
      }
    };
    loadBooking();
  }, [bookingNumber]);

  useEffect(() => {
    if (booking && !firedRef.current) {
      firedRef.current = true;
      confetti({ particleCount: 140, spread: 80, origin: { y: 0.55 }, colors: ['#6366f1', '#8b5cf6', '#a78bfa', '#c4b5fd', '#ffffff'] });
    }
  }, [booking]);

  if (loading) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', p: 4 }}>
      <Box sx={{ width: '100%', maxWidth: 360 }}>
        <Skeleton variant="circular" width={80} height={80} sx={{ mx: 'auto', mb: 2 }} />
        <Skeleton height={36} width="75%" sx={{ mx: 'auto', borderRadius: 2, mb: 3 }} />
        <Skeleton variant="rectangular" height={260} sx={{ borderRadius: 3 }} />
      </Box>
    </Box>
  );

  if (error) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="error">{error}</Typography>
    </Box>
  );

  if (!booking) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="text.secondary">Booking not found</Typography>
    </Box>
  );

  const detailRows = [
    { icon: Ticket, label: 'Movie', value: booking.movieTitle },
    { icon: Calendar, label: 'Showtime', value: formatDateTime(booking.showtimeStart) },
    { icon: MapPin, label: 'Hall', value: booking.hallName },
    { icon: Armchair, label: 'Seats', value: booking.seatNumbers.join(', ') },
    ...(booking.carLicensePlate ? [{ icon: Car, label: 'Parking', value: booking.carLicensePlate }] : []),
  ];

  return (
    <Box sx={{ minHeight: '100vh', py: 8, px: 2 }}>
      <Container maxWidth="xs">
        {/* Success icon */}
        <motion.div
          initial={{ scale: 0.5, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ type: 'spring', stiffness: 260, damping: 18 }}
          style={{ display: 'flex', justifyContent: 'center', marginBottom: 20 }}
        >
          <Box sx={{ width: 80, height: 80, borderRadius: '50%', bgcolor: 'rgba(16,185,129,0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#10b981' }}>
            <CheckCircle2 size={48} />
          </Box>
        </motion.div>

        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.15 }}>
          <Typography variant="h5" fontWeight={700} textAlign="center" mb={0.5}>Booking Confirmed!</Typography>
          <Typography variant="body2" color="text.secondary" textAlign="center" mb={4}>
            Your tickets have been booked successfully.
          </Typography>

          {/* Ticket card */}
          <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden', boxShadow: 4, mb: 3 }}>
            {/* Top stripe */}
            <Box sx={{ background: 'linear-gradient(90deg, #6366f1, #9333ea)', px: 3, py: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Ticket size={20} color="rgba(255,255,255,0.6)" />
              <Typography sx={{ fontFamily: 'monospace', color: '#fff', fontWeight: 700, letterSpacing: 2, fontSize: 14 }}>
                {booking.bookingNumber}
              </Typography>
            </Box>

            {/* Dotted separator */}
            <Box sx={{ position: 'relative', height: 24, bgcolor: 'background.default' }}>
              <Box sx={{ position: 'absolute', left: -12, top: '50%', transform: 'translateY(-50%)', width: 24, height: 24, borderRadius: '50%', bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }} />
              <Box sx={{ position: 'absolute', left: 12, right: 12, top: '50%', borderTop: '2px dashed', borderColor: 'divider' }} />
              <Box sx={{ position: 'absolute', right: -12, top: '50%', transform: 'translateY(-50%)', width: 24, height: 24, borderRadius: '50%', bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }} />
            </Box>

            {/* Details */}
            <Box sx={{ px: 3, py: 2.5 }}>
              <Stack spacing={2}>
                {detailRows.map(({ icon: Icon, label, value }) => (
                  <Box key={label} sx={{ display: 'flex', gap: 1.5 }}>
                    <Box sx={{ color: 'primary.main', mt: 0.25, flexShrink: 0 }}><Icon size={15} /></Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">{label}</Typography>
                      <Typography fontWeight={500}>{value}</Typography>
                    </Box>
                  </Box>
                ))}
              </Stack>

              <Divider sx={{ my: 2.5 }} />

              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                  <Receipt size={14} /> Total Paid
                </Typography>
                <Typography variant="h6" fontWeight={700} color="primary.main">
                  {formatCurrency(booking.totalAmount)}
                </Typography>
              </Box>
            </Box>
          </Paper>

          <Stack spacing={1.5}>
            <MuiButton component={Link} to="/my-bookings" variant="contained" fullWidth size="large">
              View My Bookings
            </MuiButton>
            <MuiButton component={Link} to="/movies" variant="outlined" fullWidth size="large">
              Browse More Movies
            </MuiButton>
          </Stack>
        </motion.div>
      </Container>
    </Box>
  );
};
