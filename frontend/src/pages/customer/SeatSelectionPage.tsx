import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { SeatMap } from '../../components/SeatMap/SeatMap';
import { BookingTimer } from '../../components/BookingTimer/BookingTimer';
import { bookingApi } from '../../api/bookingApi';
import { showtimeApi } from '../../api/showtimeApi';
import { useSeatSelection } from '../../hooks/useSeatSelection';
import type { SeatAvailabilityDto, ReservationDto, ShowtimeDto, SeatDto } from '../../types';
import { formatCurrency, formatDateTime } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

export const SeatSelectionPage: React.FC = () => {
  const { showtimeId } = useParams<{ showtimeId: string }>();
  const navigate = useNavigate();

  const [showtime, setShowtime] = useState<ShowtimeDto | null>(null);
  const [availability, setAvailability] = useState<SeatAvailabilityDto | null>(null);
  const [reservation, setReservation] = useState<ReservationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const { selectedSeats, toggleSeat, clearSelection } = useSeatSelection();

  useEffect(() => {
    const loadData = async () => {
      if (!showtimeId) return;
      try {
        setLoading(true);
        const [showtimeData, availabilityData] = await Promise.all([
          showtimeApi.getById(showtimeId),
          bookingApi.getSeatAvailability(showtimeId),
        ]);
        setShowtime(showtimeData);
        setAvailability(availabilityData);
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

  const handleReserve = async () => {
    if (selectedSeats.length === 0 || !showtimeId) return;

    try {
      const res = await bookingApi.createReservation(showtimeId, selectedSeats);
      setReservation(res);
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to reserve seats. Please try again.');
      setError(message);
      await reloadAvailability();
      clearSelection();
    }
  };

  const handleReservationExpire = async () => {
    setReservation(null);
    clearSelection();
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

  const allSeats: readonly SeatDto[] = [
    ...availability.availableSeats,
    ...availability.reservedSeats,
    ...availability.bookedSeats,
  ];

  const totalPrice = selectedSeats.reduce((sum, seatNumber) => {
    const seat = allSeats.find((s) => s.seatNumber === seatNumber);
    return sum + (seat?.price ?? 0);
  }, 0);

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
          onSeatClick={toggleSeat}
        />

        <div className="booking-summary">
          <h3>Booking Summary</h3>
          <p>
            <strong>Selected Seats:</strong>{' '}
            {selectedSeats.length > 0 ? selectedSeats.join(', ') : 'None'}
          </p>
          <p>
            <strong>Total:</strong> {formatCurrency(totalPrice)}
          </p>

          {!reservation && (
            <button
              onClick={handleReserve}
              disabled={selectedSeats.length === 0}
              className="btn btn-primary"
            >
              Reserve Seats ({selectedSeats.length})
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
