import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { bookingApi } from '../../api/bookingApi';
import type { BookingDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

export const ConfirmationPage: React.FC = () => {
  const { t } = useTranslation('customer');
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
      } catch (err: unknown) {
        const message = extractErrorMessage(err, 'Failed to load booking details');
        setError(message);
      } finally {
        setLoading(false);
      }
    };
    loadBooking();
  }, [bookingNumber]);

  if (loading) return <div className="page"><div className="loading">Loading...</div></div>;
  if (error) return <div className="page"><div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div></div>;
  if (!booking) return <div className="page"><div className="error-message">{t('confirmation.notFound')}</div></div>;

  return (
    <div className="page">
      <div className="container container-sm">
        <div className="confirmation">
          <div className="confirmation-icon">✅</div>
          <h1>{t('confirmation.bookingConfirmed')}</h1>
          <p className="confirmation-subtitle">
            {t('confirmation.successMessage')}
          </p>

          <div className="confirmation-details">
            <div className="detail-row">
              <span className="detail-label">{t('confirmation.bookingNumber')}</span>
              <span className="detail-value">{booking.bookingNumber}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">{t('confirmation.movie')}</span>
              <span className="detail-value">{booking.movieTitle}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">{t('confirmation.showtime')}</span>
              <span className="detail-value">{formatDateTime(booking.showtimeStart)}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">{t('confirmation.hall')}</span>
              <span className="detail-value">{booking.hallName}</span>
            </div>
            <div className="detail-row">
              <span className="detail-label">{t('confirmation.seats')}</span>
              <span className="detail-value">{booking.seatNumbers.join(', ')}</span>
            </div>
            <div className="detail-row detail-total">
              <span className="detail-label">{t('confirmation.totalPaid')}</span>
              <span className="detail-value">{formatCurrency(booking.totalAmount)}</span>
            </div>
            {booking.carLicensePlate && (
              <div className="detail-row">
                <span className="detail-label">{t('confirmation.parking')}</span>
                <span className="detail-value">🅿️ {booking.carLicensePlate}</span>
              </div>
            )}
          </div>

          <div className="confirmation-actions">
            <Link to="/my-bookings" className="btn btn-primary">
              {t('confirmation.viewMyBookings')}
            </Link>
            <Link to="/movies" className="btn btn-outline">
              {t('confirmation.browseMoreMovies')}
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
