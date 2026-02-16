import React, { useEffect, useState } from 'react';
import { bookingApi } from '../../api/bookingApi';
import type { BookingDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';

export const MyBookingsPage: React.FC = () => {
  const [bookings, setBookings] = useState<readonly BookingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadBookings = async () => {
      try {
        const data = await bookingApi.getMyBookings();
        setBookings(data);
      } catch {
        setError('Failed to load bookings');
      } finally {
        setLoading(false);
      }
    };
    loadBookings();
  }, []);

  const handleCancel = async (bookingId: string) => {
    if (!window.confirm('Are you sure you want to cancel this booking?')) return;

    try {
      const updated = await bookingApi.cancelBooking(bookingId);
      setBookings((prev) =>
        prev.map((b) => (b.id === bookingId ? updated : b)),
      );
    } catch {
      setError('Failed to cancel booking');
    }
  };

  if (loading) return <div className="page"><div className="loading">Loading bookings...</div></div>;
  if (error) return <div className="page"><div className="error-message">{error}</div></div>;

  return (
    <div className="page">
      <div className="container">
        <h1>My Bookings</h1>

        {bookings.length === 0 ? (
          <p className="empty-state">You haven&apos;t made any bookings yet.</p>
        ) : (
          <div className="bookings-list">
            {bookings.map((booking) => (
              <div key={booking.id} className="booking-card">
                <div className="booking-card-header">
                  <h3>{booking.movieTitle}</h3>
                  <span className={`status-badge status-${booking.status.toLowerCase()}`}>
                    {booking.status}
                  </span>
                </div>
                <div className="booking-card-body">
                  <div className="booking-detail">
                    <span>Booking #</span>
                    <strong>{booking.bookingNumber}</strong>
                  </div>
                  <div className="booking-detail">
                    <span>Showtime</span>
                    <strong>{formatDateTime(booking.showtimeStart)}</strong>
                  </div>
                  <div className="booking-detail">
                    <span>Hall</span>
                    <strong>{booking.hallName}</strong>
                  </div>
                  <div className="booking-detail">
                    <span>Seats</span>
                    <strong>{booking.seatNumbers.join(', ')}</strong>
                  </div>
                  <div className="booking-detail">
                    <span>Total</span>
                    <strong>{formatCurrency(booking.totalAmount)}</strong>
                  </div>
                </div>
                {booking.status === 'Confirmed' && (
                  <div className="booking-card-actions">
                    <button
                      onClick={() => handleCancel(booking.id)}
                      className="btn btn-danger"
                    >
                      Cancel Booking
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
