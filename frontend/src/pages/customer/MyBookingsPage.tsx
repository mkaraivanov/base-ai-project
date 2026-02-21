import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Calendar, MapPin, Armchair, Receipt, Car, Gift, Ticket, X } from 'lucide-react';
import { toast } from 'sonner';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
import MuiTabs from '@mui/material/Tabs';
import MuiTab from '@mui/material/Tab';
import LinearProgress from '@mui/material/LinearProgress';
import Chip from '@mui/material/Chip';
import Skeleton from '@mui/material/Skeleton';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import { useTranslation } from 'react-i18next';
import { bookingApi } from '../../api/bookingApi';
import { loyaltyApi } from '../../api/loyaltyApi';
import type { BookingDto, LoyaltyCardDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';
import { AlertDialog } from '../../components/ui/alert-dialog';

export const MyBookingsPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const [bookings, setBookings] = useState<readonly BookingDto[]>([]);
  const [loyaltyCard, setLoyaltyCard] = useState<LoyaltyCardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancelId, setCancelId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState(0);

  useEffect(() => {
    const loadData = async () => {
      try {
        const [bookingsData, loyaltyData] = await Promise.all([
          bookingApi.getMyBookings(),
          loyaltyApi.getMyCard().catch(() => null),
        ]);
        setBookings(bookingsData);
        setLoyaltyCard(loyaltyData);
      } catch (err: unknown) {
        toast.error(extractErrorMessage(err, t('myBookings.failedToLoad')));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  const handleConfirmCancel = async () => {
    if (!cancelId) return;
    try {
      const updated = await bookingApi.cancelBooking(cancelId);
      setBookings(prev => prev.map(b => (b.id === cancelId ? updated : b)));
      toast.success(t('myBookings.cancelledSuccess'));
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err, t('myBookings.failedToCancel')));
    } finally {
      setCancelId(null);
    }
  };

  const stampsRequired = loyaltyCard?.stampsRequired ?? 5;
  const stamps = loyaltyCard?.stamps ?? 0;
  const progressPercent = Math.min(100, (stamps / stampsRequired) * 100);

  const now = Date.now();
  const upcoming = bookings.filter(b => b.status !== 'Cancelled' && new Date(b.showtimeStart).getTime() > now);
  const past = bookings.filter(b => b.status === 'Cancelled' || new Date(b.showtimeStart).getTime() <= now);
  const tabList = [upcoming, past];

  if (loading) return (
    <Box sx={{ minHeight: '100vh', p: 3 }}>
      <Skeleton variant="rectangular" height={140} sx={{ borderRadius: 3, mb: 3 }} />
      <Stack spacing={1.5}>
        {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} height={110} sx={{ borderRadius: 3 }} />)}
      </Stack>
    </Box>
  );

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AlertDialog
        open={!!cancelId}
        onOpenChange={open => { if (!open) setCancelId(null); }}
        title="Cancel Booking?"
        description="This action cannot be undone. Your seats will be released."
        confirmLabel="Yes, Cancel"
        cancelLabel="Keep Booking"
        variant="destructive"
        onConfirm={handleConfirmCancel}
      />

      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" fontWeight={700} mb={4}>{t('myBookings.title')}</Typography>

        {/* Loyalty card */}
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <Box sx={{
            position: 'relative', overflow: 'hidden', borderRadius: 3,
            background: 'linear-gradient(135deg, #4338ca 0%, #7c3aed 60%, #3730a3 100%)',
            p: 3, mb: 5, color: '#fff',
            boxShadow: '0 8px 32px rgba(99,102,241,0.3)',
          }}>
            <Box sx={{ position: 'absolute', right: -24, top: -24, width: 160, height: 160, borderRadius: '50%', bgcolor: 'rgba(255,255,255,0.05)' }} />
            <Box sx={{ position: 'absolute', right: 12, top: 40, width: 96, height: 96, borderRadius: '50%', bgcolor: 'rgba(255,255,255,0.05)' }} />

            <Box sx={{ position: 'relative' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2.5 }}>
                <Box>
                  <Typography fontWeight={700} fontSize={18}>{t('myBookings.loyaltyProgram')}</Typography>
                  <Typography sx={{ color: '#c7d2fe', fontSize: 14 }}>{t('myBookings.moviesWatched', { stamps, total: stampsRequired })}</Typography>
                </Box>
                <Ticket size={28} color="rgba(255,255,255,0.35)" />
              </Box>

              <LinearProgress
                variant="determinate"
                value={progressPercent}
                sx={{ height: 8, borderRadius: 4, mb: 2.5, bgcolor: 'rgba(255,255,255,0.2)', '& .MuiLinearProgress-bar': { bgcolor: '#fff' } }}
              />

              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 1 }}>
                {Array.from({ length: stampsRequired }).map((_, i) => (
                  <Box
                    key={i}
                    sx={{
                      width: 32, height: 32, borderRadius: '50%', border: '2px solid',
                      borderColor: i < stamps ? '#fff' : 'rgba(255,255,255,0.3)',
                      bgcolor: i < stamps ? '#fff' : 'transparent',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: 14,
                    }}
                  >
                    {i < stamps ? '🎟' : ''}
                  </Box>
                ))}
              </Box>

              {loyaltyCard && loyaltyCard.stampsRemaining > 0 && (
                <Typography sx={{ color: '#c7d2fe', fontSize: 12 }}>
                  {t('myBookings.moreToFreeTicket', { count: loyaltyCard.stampsRemaining })}
                </Typography>
              )}

              {loyaltyCard && loyaltyCard.activeVouchers.length > 0 && (
                <Box sx={{ mt: 2.5, pt: 2.5, borderTop: '1px solid rgba(255,255,255,0.2)' }}>
                  <Typography sx={{ fontSize: 14, fontWeight: 600, mb: 1, display: 'flex', alignItems: 'center', gap: 0.75 }}>
                    <Gift size={14} /> {t('myBookings.freeTicketVouchers')}
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {loyaltyCard.activeVouchers.map(v => (
                      <Box key={v.id} sx={{ fontFamily: 'monospace', bgcolor: 'rgba(255,255,255,0.15)', px: 1.5, py: 0.5, borderRadius: 1.5, fontWeight: 700, letterSpacing: 2, fontSize: 13 }}>
                        {v.code}
                      </Box>
                    ))}
                  </Box>
                </Box>
              )}
            </Box>
          </Box>
        </motion.div>

        {/* Tabs */}
        <MuiTabs value={activeTab} onChange={(_, v) => setActiveTab(v)} sx={{ mb: 3 }}>
          <MuiTab label={<Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>Upcoming <Chip label={upcoming.length} size="small" sx={{ ml: 0.5, height: 20, fontSize: 11 }} /></Box>} />
          <MuiTab label={<Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>Past &amp; Cancelled <Chip label={past.length} size="small" sx={{ ml: 0.5, height: 20, fontSize: 11 }} /></Box>} />
        </MuiTabs>

        {tabList[activeTab].length === 0 ? (
          <Typography className="empty-state" color="text.secondary" textAlign="center" py={6}>
            {t('myBookings.noBookings')}
          </Typography>
        ) : (
          <Stack className="bookings-list" spacing={2}>
            {tabList[activeTab].map(booking => (
              <motion.div key={booking.id} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }}>
                <Paper className="booking-card" variant="outlined" sx={{ borderRadius: 3, p: 2.5 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2, gap: 1 }}>
                    <Typography fontWeight={600}>{booking.movieTitle}</Typography>
                    <Chip
                      label={booking.status}
                      size="small"
                      color={booking.status === 'Confirmed' ? 'success' : booking.status === 'Cancelled' ? 'error' : 'default'}
                      sx={{ flexShrink: 0 }}
                    />
                  </Box>

                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
                    {[
                      { Icon: Receipt, text: `#${booking.bookingNumber}` },
                      { Icon: Calendar, text: formatDateTime(booking.showtimeStart) },
                      { Icon: MapPin, text: booking.hallName },
                      { Icon: Armchair, text: booking.seatNumbers.join(', ') },
                      ...(booking.carLicensePlate ? [{ Icon: Car, text: booking.carLicensePlate }] : []),
                    ].map(({ Icon, text }) => (
                      <Typography key={text} variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                        <Icon size={12} /> {text}
                      </Typography>
                    ))}
                  </Box>

                  <Divider sx={{ my: 2 }} />

                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography fontWeight={700} color="primary.main">{formatCurrency(booking.totalAmount)}</Typography>
                    {booking.status === 'Confirmed' &&
                      new Date(booking.showtimeStart).getTime() - Date.now() > 60 * 60 * 1000 && (
                        <MuiButton
                          variant="outlined"
                          size="small"
                          color="error"
                          startIcon={<X size={13} />}
                          onClick={() => setCancelId(booking.id)}
                        >
                          {t('myBookings.cancelBooking')}
                        </MuiButton>
                      )}
                  </Box>
                </Paper>
              </motion.div>
            ))}
          </Stack>
        )}
      </Container>
    </Box>
  );
};
