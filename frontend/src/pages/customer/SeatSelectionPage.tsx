import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import MuiButton from '@mui/material/Button';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Divider from '@mui/material/Divider';
import Alert from '@mui/material/Alert';
import Chip from '@mui/material/Chip';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import { SeatMap } from '../../components/SeatMap/SeatMap';
import { BookingTimer } from '../../components/BookingTimer/BookingTimer';
import { bookingApi } from '../../api/bookingApi';
import { ticketTypeApi } from '../../api/ticketTypeApi';
import { showtimeApi } from '../../api/showtimeApi';
import { useSeatSelection } from '../../hooks/useSeatSelection';
import type { SeatAvailabilityDto, ReservationDto, ShowtimeDto, SeatDto, TicketTypeDto } from '../../types';
import { formatCurrency, formatDateTime } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

interface SeatTicketSelection {
  ticketTypeId: string;
  ticketTypeName: string;
  seatPrice: number;
  unitPrice: number;
}

export const SeatSelectionPage: React.FC = () => {
  const { showtimeId } = useParams<{ showtimeId: string }>();
  const navigate = useNavigate();
  const [showtime, setShowtime] = useState<ShowtimeDto | null>(null);
  const [availability, setAvailability] = useState<SeatAvailabilityDto | null>(null);
  const [ticketTypes, setTicketTypes] = useState<readonly TicketTypeDto[]>([]);
  const [reservation, setReservation] = useState<ReservationDto | null>(null);
  const [seatTickets, setSeatTickets] = useState<Map<string, SeatTicketSelection>>(new Map());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { selectedSeats, toggleSeat, clearSelection } = useSeatSelection();
  const defaultTicketType = ticketTypes.find(t => t.isActive) ?? null;

  useEffect(() => {
    const loadData = async () => {
      if (!showtimeId) return;
      try {
        setLoading(true);
        const [showtimeData, availabilityData, ticketTypeData] = await Promise.all([
          showtimeApi.getById(showtimeId),
          bookingApi.getSeatAvailability(showtimeId),
          ticketTypeApi.getActive(),
        ]);
        setShowtime(showtimeData);
        setAvailability(availabilityData);
        setTicketTypes(ticketTypeData);
      } catch (err: unknown) {
        setError(extractErrorMessage(err, 'Failed to load seat availability'));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [showtimeId]);

  const reloadAvailability = async () => {
    if (!showtimeId) return;
    try {
      const data = await bookingApi.getSeatAvailability(showtimeId);
      setAvailability(data);
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to reload seat availability'));
    }
  };

  const allSeats: readonly SeatDto[] = availability
    ? [...availability.availableSeats, ...availability.reservedSeats, ...availability.bookedSeats]
    : [];

  const handleToggleSeat = useCallback(
    (seatNumber: string) => {
      const wasSelected = selectedSeats.includes(seatNumber);
      toggleSeat(seatNumber);
      if (wasSelected) {
        setSeatTickets(prev => { const next = new Map(prev); next.delete(seatNumber); return next; });
      } else if (defaultTicketType) {
        const seat = allSeats.find(s => s.seatNumber === seatNumber);
        const seatPrice = seat?.price ?? 0;
        setSeatTickets(prev => {
          const next = new Map(prev);
          next.set(seatNumber, {
            ticketTypeId: defaultTicketType.id,
            ticketTypeName: defaultTicketType.name,
            seatPrice,
            unitPrice: Math.round(seatPrice * defaultTicketType.priceModifier * 100) / 100,
          });
          return next;
        });
      }
    },
    [selectedSeats, toggleSeat, defaultTicketType, allSeats],
  );

  const handleTicketTypeChange = (seatNumber: string, ticketTypeId: string) => {
    const ticketType = ticketTypes.find(t => t.id === ticketTypeId);
    if (!ticketType) return;
    const seatPrice = seatTickets.get(seatNumber)?.seatPrice ?? 0;
    setSeatTickets(prev => {
      const next = new Map(prev);
      next.set(seatNumber, {
        ticketTypeId: ticketType.id,
        ticketTypeName: ticketType.name,
        seatPrice,
        unitPrice: Math.round(seatPrice * ticketType.priceModifier * 100) / 100,
      });
      return next;
    });
  };

  const totalPrice = Array.from(seatTickets.values()).reduce((sum, sel) => sum + sel.unitPrice, 0);

  const handleReserve = async () => {
    if (selectedSeats.length === 0 || !showtimeId) return;
    const seats = selectedSeats.map(seatNumber => ({
      seatNumber,
      ticketTypeId: seatTickets.get(seatNumber)?.ticketTypeId ?? defaultTicketType?.id ?? '',
    }));
    try {
      const res = await bookingApi.createReservation(showtimeId, seats);
      setReservation(res);
    } catch (err: unknown) {
      setError(extractErrorMessage(err, 'Failed to reserve seats. Please try again.'));
      await reloadAvailability();
      clearSelection();
      setSeatTickets(new Map());
    }
  };

  const handleReservationExpire = async () => {
    setReservation(null);
    clearSelection();
    setSeatTickets(new Map());
    await reloadAvailability();
    setError('Your reservation has expired. Please select seats again.');
  };

  if (loading) return (
    <Box sx={{ minHeight: '100vh', p: 4 }}>
      <Skeleton height={40} width={256} sx={{ borderRadius: 2, mb: 1 }} />
      <Skeleton height={20} width={160} sx={{ borderRadius: 1, mb: 4 }} />
      <Skeleton variant="rectangular" height={280} sx={{ maxWidth: 640, mx: 'auto', borderRadius: 3 }} />
    </Box>
  );

  if (error && !availability) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="error" sx={{ whiteSpace: 'pre-line' }}>{error}</Typography>
    </Box>
  );

  if (!availability) return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Typography color="text.secondary">No data available</Typography>
    </Box>
  );

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <Paper variant="outlined" square elevation={0} sx={{ position: 'sticky', top: 64, zIndex: 30, borderTop: 'none', borderLeft: 'none', borderRight: 'none' }}>
        <Container maxWidth="lg" sx={{ py: 1.5 }}>
          {showtime && (
            <Box>
              <Typography variant="h6" fontWeight={700}>{showtime.movieTitle}</Typography>
              <Typography variant="body2" color="text.secondary">
                {formatDateTime(showtime.startTime)} &bull; {showtime.hallName}
              </Typography>
            </Box>
          )}
        </Container>
      </Paper>

      <Container maxWidth="lg" sx={{ py: 3 }}>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 300px' }, gap: 3 }}>
          <Box>
            {error && (
              <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-line' }}>{error}</Alert>
            )}
            {reservation && (
              <Paper variant="outlined" sx={{ p: 2, mb: 2, borderRadius: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                <BookingTimer expiresAt={new Date(reservation.expiresAt)} onExpire={handleReservationExpire} />
                <MuiButton variant="contained" size="small" onClick={() => navigate(`/checkout/${reservation.id}`)}>
                  Proceed to Checkout
                </MuiButton>
              </Paper>
            )}
            <SeatMap seats={allSeats} selectedSeats={[...selectedSeats]} onSeatClick={handleToggleSeat} />
          </Box>

          <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3, alignSelf: 'start', position: { md: 'sticky' }, top: { md: 160 } }}>
            <Typography fontWeight={600} mb={2}>Booking Summary</Typography>
            {selectedSeats.length === 0 ? (
              <Typography variant="body2" color="text.secondary">Select seats on the map to begin.</Typography>
            ) : (
              <>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, mb: 2 }}>
                  {selectedSeats.map(seatNumber => {
                    const sel = seatTickets.get(seatNumber);
                    return (
                      <Box key={seatNumber} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Chip label={seatNumber} size="small" variant="outlined" sx={{ fontFamily: 'monospace', fontWeight: 500, minWidth: 48 }} />
                        <Box sx={{ flex: 1, minWidth: 0 }}>
                          {ticketTypes.length > 1 ? (
                            <FormControl size="small" fullWidth disabled={!!reservation}>
                              <InputLabel sx={{ fontSize: 12 }}>Type</InputLabel>
                              <Select
                                value={sel?.ticketTypeId ?? ''}
                                label="Type"
                                onChange={e => handleTicketTypeChange(seatNumber, e.target.value)}
                                sx={{ fontSize: 12 }}
                              >
                                {ticketTypes.map(tt => (
                                  <MenuItem key={tt.id} value={tt.id} sx={{ fontSize: 12 }}>{tt.name}</MenuItem>
                                ))}
                              </Select>
                            </FormControl>
                          ) : (
                            <Typography variant="caption" color="text.secondary">{sel?.ticketTypeName ?? '—'}</Typography>
                          )}
                        </Box>
                        <Box sx={{ flexShrink: 0, textAlign: 'right' }}>
                          {sel ? (
                            <Box>
                              {sel.unitPrice < sel.seatPrice && (
                                <Typography variant="caption" color="text.secondary" sx={{ textDecoration: 'line-through', display: 'block' }}>
                                  {formatCurrency(sel.seatPrice)}
                                </Typography>
                              )}
                              <Typography variant="body2" fontWeight={500}>{formatCurrency(sel.unitPrice)}</Typography>
                            </Box>
                          ) : '—'}
                        </Box>
                      </Box>
                    );
                  })}
                </Box>

                <Divider sx={{ mb: 2 }} />

                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                  <Typography fontWeight={600}>Total</Typography>
                  <Typography variant="h6" fontWeight={700} color="primary.main">{formatCurrency(totalPrice)}</Typography>
                </Box>
                {!reservation && (
                  <MuiButton variant="contained" fullWidth onClick={handleReserve} disabled={selectedSeats.length === 0}>
                    Reserve {selectedSeats.length} Seat{selectedSeats.length !== 1 ? 's' : ''}
                  </MuiButton>
                )}
              </>
            )}
          </Paper>
        </Box>
      </Container>
    </Box>
  );
};
