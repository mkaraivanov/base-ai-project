import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import MuiButton from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import Select, { SelectChangeEvent } from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import InputLabel from '@mui/material/InputLabel';
import FormControl from '@mui/material/FormControl';
import Grid from '@mui/material/Grid';
import Divider from '@mui/material/Divider';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Skeleton from '@mui/material/Skeleton';
import Alert from '@mui/material/Alert';
import { CreditCard, Ticket, Lock, Car } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { bookingApi } from '../../api/bookingApi';
import type { ReservationDto } from '../../types';
import { formatCurrency, formatDateTime } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';
import { toast } from 'sonner';

type PaymentMethod = 'card' | 'paypal' | 'applepay';

interface CheckoutForm {
  paymentMethod: PaymentMethod;
  cardholderName: string;
  cardNumber: string;
  expiryDate: string;
  cvv: string;
  parkingPlate: string;
}

const EMPTY: CheckoutForm = {
  paymentMethod: 'card',
  cardholderName: '',
  cardNumber: '',
  expiryDate: '',
  cvv: '',
  parkingPlate: '',
};

export const CheckoutPage: React.FC = () => {
  const { t } = useTranslation('customer');
  const { reservationId } = useParams<{ reservationId: string }>();
  const navigate = useNavigate();
  const [reservation, setReservation] = useState<ReservationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<CheckoutForm>(EMPTY);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    const load = async () => {
      if (!reservationId) return;
      try {
        setLoading(true);
        const res = await bookingApi.getReservation(reservationId);
        setReservation(res);
      } catch (err: unknown) {
        setError(extractErrorMessage(err, t('checkout.loadFailed')));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [reservationId]);

  const set = (name: keyof CheckoutForm, value: string) =>
    setForm(prev => ({ ...prev, [name]: value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reservationId) return;
    setSubmitting(true);
    try {
      const booking = await bookingApi.confirmBooking(reservationId, {
        paymentMethod: form.paymentMethod,
        parkingPlate: form.parkingPlate || null,
      });
      toast.success(t('checkout.bookingConfirmed'));
      navigate(`/confirmation/${booking.id}`);
    } catch (err: unknown) {
      setError(extractErrorMessage(err, t('checkout.paymentFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return (
    <Container maxWidth="md" sx={{ py: 6 }}>
      <Skeleton height={40} sx={{ mb: 2 }} />
      <Stack spacing={2}>{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} height={56} sx={{ borderRadius: 2 }} />)}</Stack>
    </Container>
  );

  if (error && !reservation) return (
    <Container maxWidth="md" sx={{ py: 6 }}>
      <Alert severity="error">{error}</Alert>
    </Container>
  );

  const totalPrice = reservation?.totalPrice ?? 0;

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Typography variant="h5" fontWeight={700} mb={4}>{t('checkout.title')}</Typography>

        {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 340px' }, gap: 3 }}>
          {/* Payment form */}
          <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}>
            <Paper variant="outlined" component="form" onSubmit={handleSubmit} sx={{ borderRadius: 3, p: 3 }}>
              <Typography fontWeight={600} mb={3} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CreditCard size={20} /> Payment Details
              </Typography>

              <Stack spacing={2.5}>
                <FormControl fullWidth>
                  <InputLabel>{t('checkout.paymentMethod')}</InputLabel>
                  <Select
                    value={form.paymentMethod}
                    label={t('checkout.paymentMethod')}
                    onChange={(e: SelectChangeEvent) => set('paymentMethod', e.target.value)}
                  >
                    <MenuItem value="card">{t('checkout.creditCard')} / {t('checkout.debitCard')}</MenuItem>
                    <MenuItem value="paypal">PayPal</MenuItem>
                    <MenuItem value="applepay">Apple Pay</MenuItem>
                  </Select>
                </FormControl>

                {form.paymentMethod === 'card' && (
                  <>
                    <TextField
                      label={t('checkout.cardHolderName')}
                      value={form.cardholderName}
                      onChange={e => set('cardholderName', e.target.value)}
                      required
                      fullWidth
                      placeholder="John Smith"
                    />
                    <TextField
                      label={t('checkout.cardNumber')}
                      value={form.cardNumber}
                      onChange={e => set('cardNumber', e.target.value.replace(/\D/g, '').slice(0, 16))}
                      required
                      fullWidth
                      placeholder="1234 5678 9012 3456"
                      slotProps={{ htmlInput: { maxLength: 16 } }}
                    />
                    <Grid container spacing={2}>
                      <Grid size={6}>
                        <TextField
                          label={t('checkout.expiryDate')}
                          value={form.expiryDate}
                          onChange={e => set('expiryDate', e.target.value)}
                          required
                          fullWidth
                          placeholder="MM/YY"
                          slotProps={{ htmlInput: { maxLength: 5 } }}
                        />
                      </Grid>
                      <Grid size={6}>
                        <TextField
                          label={t('checkout.cvv')}
                          value={form.cvv}
                          onChange={e => set('cvv', e.target.value.replace(/\D/g, '').slice(0, 4))}
                          required
                          fullWidth
                          type="password"
                          placeholder="•••"
                          slotProps={{ htmlInput: { maxLength: 4 } }}
                        />
                      </Grid>
                    </Grid>
                  </>
                )}

                <Divider />

                <TextField
                  label={`${t('checkout.carLicensePlate')} ${t('checkout.carLicensePlateOptional')}`}
                  value={form.parkingPlate}
                  onChange={e => set('parkingPlate', e.target.value.toUpperCase())}
                  fullWidth
                  placeholder="AB12 CDE"
                  slotProps={{
                    input: { startAdornment: <Car size={16} style={{ marginRight: 8, opacity: 0.5 }} /> },
                  }}
                />

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: 'text.secondary' }}>
                  <Lock size={14} />
                  <Typography variant="caption">Your payment is encrypted and secure.</Typography>
                </Box>

                <MuiButton
                  type="submit"
                  variant="contained"
                  size="large"
                  fullWidth
                  disabled={submitting}
                  startIcon={submitting ? <CircularProgress size={18} color="inherit" /> : undefined}
                >
                  {submitting ? t('checkout.processing') : `${t('checkout.pay')} ${formatCurrency(totalPrice)}`}
                </MuiButton>
              </Stack>
            </Paper>
          </motion.div>

          {/* Order summary */}
          <Paper variant="outlined" sx={{ borderRadius: 3, p: 3, alignSelf: 'flex-start' }}>
            <Typography fontWeight={600} mb={2} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Ticket size={18} /> {t('checkout.orderSummary')}
            </Typography>
            {reservation && (
              <Stack spacing={1.5}>
                <Box>
                  <Typography fontWeight={500}>{reservation.movieTitle}</Typography>
                  <Typography variant="body2" color="text.secondary">{formatDateTime(reservation.startTime)}</Typography>
                  <Typography variant="body2" color="text.secondary">{reservation.hallName}</Typography>
                </Box>
                <Divider />
                {reservation.seats?.map((seat) => (
                  <Box key={seat.seatId} sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="body2">{t('checkout.seats')} {seat.seatNumber} · {seat.ticketTypeName}</Typography>
                    <Typography variant="body2">{formatCurrency(seat.price)}</Typography>
                  </Box>
                ))}
                <Divider />
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography fontWeight={700}>{t('checkout.total')}</Typography>
                  <Typography fontWeight={700} color="primary.main">{formatCurrency(totalPrice)}</Typography>
                </Box>
              </Stack>
            )}
          </Paper>
        </Box>
      </Container>
    </Box>
  );
};
