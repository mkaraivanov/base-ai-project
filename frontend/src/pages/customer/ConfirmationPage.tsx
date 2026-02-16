import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { bookingApi } from '../../api/bookingApi';
import type { BookingDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';

export const ConfirmationPage: React.FC = () => {
  const { bookingNumber } = useParams<{ bookingNumber: string }>();
  const [booking, setBooking] = useState<BookingDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadBooking = async () => {
      if (!bookingNumber) return;
      try {
        const data = await bookingApi.getByBookingNumber(bookingNumber);
        setBooking(data);
      } catch {
        setError('Failed to load booking details');
      } finally {
        setLoading(false);
      }
    };
    loadBooking();
  }, [bookingNumber]);

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;
  if (error) return <div className="page"><div className="error-message">{error}</div></div>;
  if (!booking) return <div className="page"><div className="error-message">Booking not found</div></div>;

  return (
    <div className="page">
      <div className="container container-sm">
        <div className="confirmation">
          <div className="confirmation-icon">✅</div>
          <h1>Booking Confirmed!</h1>
          <p className="confirmation-subtitle">
            Your tickets have been booked successfully.
          </p>

          <div className="confirmation-details">
            <div className="detail-row">
              <span className="detail-label">Booking Number</span>
              <span className="detail-value">{booking.bookingNumber}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Movie</span>
              <span className="detail-value">{booking.movieTitle}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Showtime</span>
              <span className="detail-value">{formatDateTime(booking.showtimeStart)}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Hall</span>
              <span className="detail-value">{booking.hallName}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">Seats</span>
              <span className="detail-value">{booking.seatNumbers.join(', ')}</span>
            </div>
            <div className="detail-row detail-total">
              <span className="detail-label">Total Paid</span>
              <span className="detail-value">{formatCurrency(booking.totalAmount)}</span>
            </div>
          </div>

          <div className="confirmation-actions">
            <Link to="/my-bookings" className="btn btn-primary">
              View My Bookings
            </Link>
            <Link to="/movies" className="btn btn-outline">
              Browse More Movies
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
