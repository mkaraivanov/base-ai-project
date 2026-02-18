import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
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

  const defaultTicketType = ticketTypes.find((t) => t.isActive) ?? null;

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
        const message = extractErrorMessage(err, 'Failed to load seat availability');
        setError(message);
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
      const message = extractErrorMessage(err, 'Failed to reload seat availability');
      setError(message);
    }
  };

  const allSeats: readonly SeatDto[] = availability
    ? [
        ...availability.availableSeats,
        ...availability.reservedSeats,
        ...availability.bookedSeats,
      ]
    : [];

  const handleToggleSeat = useCallback(
    (seatNumber: string) => {
      const wasSelected = selectedSeats.includes(seatNumber);
      toggleSeat(seatNumber);

      if (wasSelected) {
        // Remove from ticket selections
        setSeatTickets((prev) => {
          const next = new Map(prev);
          next.delete(seatNumber);
          return next;
        });
      } else if (defaultTicketType) {
        // Auto-assign the default (first active) ticket type
        const seat = allSeats.find((s) => s.seatNumber === seatNumber);
        const seatPrice = seat?.price ?? 0;
        setSeatTickets((prev) => {
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
    const ticketType = ticketTypes.find((t) => t.id === ticketTypeId);
    if (!ticketType) return;
    const seatPrice = seatTickets.get(seatNumber)?.seatPrice ?? 0;
    setSeatTickets((prev) => {
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

  const totalPrice = Array.from(seatTickets.values()).reduce(
    (sum, sel) => sum + sel.unitPrice,
    0,
  );

  const handleReserve = async () => {
    if (selectedSeats.length === 0 || !showtimeId) return;

    const seats = selectedSeats.map((seatNumber) => ({
      seatNumber,
      ticketTypeId: seatTickets.get(seatNumber)?.ticketTypeId ?? defaultTicketType?.id ?? '',
    }));

    try {
      const res = await bookingApi.createReservation(showtimeId, seats);
      setReservation(res);
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to reserve seats. Please try again.');
      setError(message);
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

  const handleProceedToCheckout = () => {
    if (reservation) {
      navigate(`/checkout/${reservation.id}`);
    }
  };

  if (loading) return <div className="page"><div className="loading">Loading seats...</div></div>;
  if (error && !availability) return <div className="page"><div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div></div>;
  if (!availability) return <div className="page"><div className="error-message">No data available</div></div>;

  return (
    <div className="page">
      <div className="container">
        {showtime && (
          <div className="showtime-header">
            <h1>{showtime.movieTitle}</h1>
            <p>
              {formatDateTime(showtime.startTime)} &bull; {showtime.hallName}
            </p>
          </div>
        )}

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        {reservation && (
          <div className="reservation-banner">
            <BookingTimer
              expiresAt={new Date(reservation.expiresAt)}
              onExpire={handleReservationExpire}
            />
            <button onClick={handleProceedToCheckout} className="btn btn-primary">
              Proceed to Checkout
            </button>
          </div>
        )}

        <SeatMap
          seats={allSeats}
          selectedSeats={[...selectedSeats]}
          onSeatClick={handleToggleSeat}
        />

        <div className="booking-summary">
          <h3>Booking Summary</h3>

          {selectedSeats.length === 0 ? (
            <p>No seats selected. Click a seat on the map to begin.</p>
          ) : (
            <>
              <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '1rem' }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', paddingBottom: '0.5rem' }}>Seat</th>
                    <th style={{ textAlign: 'left', paddingBottom: '0.5rem' }}>Ticket Type</th>
                    <th style={{ textAlign: 'right', paddingBottom: '0.5rem' }}>Price</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedSeats.map((seatNumber) => {
                    const sel = seatTickets.get(seatNumber);
                    return (
                      <tr key={seatNumber}>
                        <td style={{ padding: '0.25rem 0' }}>{seatNumber}</td>
                        <td style={{ padding: '0.25rem 0.5rem' }}>
                          {ticketTypes.length > 1 ? (
                            <select
                              value={sel?.ticketTypeId ?? ''}
                              onChange={(e) => handleTicketTypeChange(seatNumber, e.target.value)}
                              className="form-control"
                              style={{ padding: '0.2rem 0.4rem', fontSize: '0.9rem' }}
                              disabled={!!reservation}
                            >
                              {ticketTypes.map((tt) => (
                                <option key={tt.id} value={tt.id}>
                                  {tt.name}
                                </option>
                              ))}
                            </select>
                          ) : (
                            <span>{sel?.ticketTypeName ?? '—'}</span>
                          )}
                        </td>
                        <td style={{ textAlign: 'right', padding: '0.25rem 0' }}>
                          {sel ? (
                            <>
                              {sel.unitPrice < sel.seatPrice && (
                                <small style={{ textDecoration: 'line-through', color: '#999', marginRight: '4px' }}>
                                  {formatCurrency(sel.seatPrice)}
                                </small>
                              )}
                              {formatCurrency(sel.unitPrice)}
                            </>
                          ) : (
                            '—'
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={2} style={{ paddingTop: '0.75rem', fontWeight: 'bold' }}>Total</td>
                    <td style={{ textAlign: 'right', paddingTop: '0.75rem', fontWeight: 'bold' }}>
                      {formatCurrency(totalPrice)}
                    </td>
                  </tr>
                </tfoot>
              </table>

              {!reservation && (
                <button
                  onClick={handleReserve}
                  disabled={selectedSeats.length === 0}
                  className="btn btn-primary"
                >
                  Reserve Seats ({selectedSeats.length})
                </button>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

