import React, { useEffect, useState } from 'react';
import { bookingApi } from '../../api/bookingApi';
import { loyaltyApi } from '../../api/loyaltyApi';
import type { BookingDto, LoyaltyCardDto } from '../../types';
import { formatDateTime, formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

export const MyBookingsPage: React.FC = () => {
  const [bookings, setBookings] = useState<readonly BookingDto[]>([]);
  const [loyaltyCard, setLoyaltyCard] = useState<LoyaltyCardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
        const message = extractErrorMessage(err, 'Failed to load bookings');
        setError(message);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  const handleCancel = async (bookingId: string) => {
    if (!window.confirm('Are you sure you want to cancel this booking?')) return;

    try {
      const updated = await bookingApi.cancelBooking(bookingId);
      setBookings((prev) =>
        prev.map((b) => (b.id === bookingId ? updated : b)),
      );
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Failed to cancel booking');
      setError(message);
    }
  };

  if (loading) return <div className="page"><div className="loading">Loading bookings...</div></div>;
  if (error) return <div className="page"><div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div></div>;

  const stampsRequired = loyaltyCard?.stampsRequired ?? 5;
  const stamps = loyaltyCard?.stamps ?? 0;
  const progressPercent = Math.min(100, (stamps / stampsRequired) * 100);

  return (
    <div className="page">
      <div className="container">
        <h1>My Bookings</h1>

        {/* Loyalty Progress Card */}
        <div className="loyalty-card" style={{
          background: 'linear-gradient(135deg, #1e40af 0%, #3b82f6 100%)',
          color: '#fff',
          borderRadius: '12px',
          padding: '24px',
          marginBottom: '32px',
          boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
            <div>
              <h2 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 700 }}>🎬 Movie Loyalty Program</h2>
              <p style={{ margin: '4px 0 0', opacity: 0.85, fontSize: '0.9rem' }}>
                {stamps} / {stampsRequired} movies watched
              </p>
            </div>
            <div style={{ textAlign: 'right', fontSize: '0.85rem', opacity: 0.85 }}>
              {loyaltyCard && loyaltyCard.stampsRemaining > 0
                ? <span>{loyaltyCard.stampsRemaining} more to free ticket</span>
                : stamps === 0 && stampsRequired > 0
                ? <span>{stampsRequired} movies for a free ticket</span>
                : null}
            </div>
          </div>

          {/* Progress bar */}
          <div style={{ background: 'rgba(255,255,255,0.25)', borderRadius: '9999px', height: '8px', overflow: 'hidden' }}>
            <div style={{
              background: '#fff',
              borderRadius: '9999px',
              height: '100%',
              width: `${progressPercent}%`,
              transition: 'width 0.4s ease',
            }} />
          </div>

          {/* Stamps visualization */}
          <div style={{ display: 'flex', gap: '8px', marginTop: '16px', flexWrap: 'wrap' }}>
            {Array.from({ length: stampsRequired }).map((_, i) => (
              <div
                key={i}
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: '50%',
                  background: i < stamps ? '#fff' : 'rgba(255,255,255,0.25)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '16px',
                  transition: 'background 0.2s',
                }}
                title={i < stamps ? 'Visited' : 'Not yet visited'}
              >
                {i < stamps ? '🎟' : ''}
              </div>
            ))}
          </div>

          {/* Active Vouchers */}
          {loyaltyCard && loyaltyCard.activeVouchers.length > 0 && (
            <div style={{ marginTop: '20px', borderTop: '1px solid rgba(255,255,255,0.3)', paddingTop: '16px' }}>
              <h3 style={{ margin: '0 0 10px', fontSize: '1rem', fontWeight: 600 }}>🎁 Your Free Ticket Vouchers</h3>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {loyaltyCard.activeVouchers.map((voucher) => (
                  <div key={voucher.id} style={{
                    background: 'rgba(255,255,255,0.15)',
                    borderRadius: '8px',
                    padding: '10px 14px',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}>
                    <span style={{ fontFamily: 'monospace', fontWeight: 700, fontSize: '1rem', letterSpacing: '0.05em' }}>
                      {voucher.code}
                    </span>
                    <span style={{ fontSize: '0.8rem', opacity: 0.85 }}>Free Ticket</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

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
