import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { bookingApi } from '../../api/bookingApi';
import { BookingTimer } from '../../components/BookingTimer/BookingTimer';
import type { ReservationDto, ConfirmBookingDto } from '../../types';
import { formatCurrency } from '../../utils/formatters';
import { extractErrorMessage } from '../../utils/errorHandler';

interface PaymentForm {
  paymentMethod: string;
  cardNumber: string;
  cardHolderName: string;
  expiryDate: string;
  cvv: string;
  carLicensePlate: string;
}

const INITIAL_FORM: PaymentForm = {
  paymentMethod: 'CreditCard',
  cardNumber: '',
  cardHolderName: '',
  expiryDate: '',
  cvv: '',
  carLicensePlate: '',
};

export const CheckoutPage: React.FC = () => {
  const { reservationId } = useParams<{ reservationId: string }>();
  const navigate = useNavigate();

  const [reservation, setReservation] = useState<ReservationDto | null>(null);
  const [form, setForm] = useState<PaymentForm>(INITIAL_FORM);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // We store the reservation info from the booking flow.
    // In a real app, we'd fetch reservation details from the API.
    // For now, we use what's available via the URL param.
    if (reservationId) {
      setReservation({
        id: reservationId,
        showtimeId: '',
        seatNumbers: [],
        totalAmount: 0,
        expiresAt: new Date(Date.now() + 10 * 60 * 1000).toISOString(),
        status: 'Pending',
        createdAt: new Date().toISOString(),
      });
    }
  }, [reservationId]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reservationId) return;

    if (!form.cardNumber || !form.cardHolderName || !form.expiryDate || !form.cvv) {
      setError('Please fill in all payment fields.');
      return;
    }

    const normalizedPlate = form.carLicensePlate.trim().toUpperCase() || undefined;
    if (normalizedPlate && !/^[A-Z]{1,2}\d{4}[A-Z]{2}$/.test(normalizedPlate)) {
      setError('Car license plate must be a valid Bulgarian format (e.g. CB1234AB).');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      // Remove spaces and dashes from card number before sending
      const cleanedCardNumber = form.cardNumber.replace(/[\s-]/g, '');

      const confirmData: ConfirmBookingDto = {
        reservationId,
        paymentMethod: form.paymentMethod,
        cardNumber: cleanedCardNumber,
        cardHolderName: form.cardHolderName,
        expiryDate: form.expiryDate,
        cvv: form.cvv,
        carLicensePlate: normalizedPlate,
      };

      const booking = await bookingApi.confirmBooking(confirmData);
      navigate(`/confirmation/${booking.bookingNumber}`);
    } catch (err: unknown) {
      const message = extractErrorMessage(err, 'Payment failed. Please try again.');
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleExpire = () => {
    navigate('/movies');
  };

  return (
    <div className="page">
      <div className="container container-sm">
        <h1>Checkout</h1>

        {reservation && (
          <BookingTimer
            expiresAt={new Date(reservation.expiresAt)}
            onExpire={handleExpire}
          />
        )}

        {reservation && reservation.totalAmount > 0 && (
          <div className="order-summary">
            <h3>Order Summary</h3>
            <p>Seats: {reservation.seatNumbers.join(', ')}</p>
            <p className="total">Total: {formatCurrency(reservation.totalAmount)}</p>
          </div>
        )}

        {error && <div className="error-message" style={{ whiteSpace: 'pre-line' }}>{error}</div>}

        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label htmlFor="paymentMethod">Payment Method</label>
            <select
              id="paymentMethod"
              name="paymentMethod"
              value={form.paymentMethod}
              onChange={handleInputChange}
              className="input"
            >
              <option value="CreditCard">Credit Card</option>
              <option value="DebitCard">Debit Card</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="cardHolderName">Card Holder Name</label>
            <input
              type="text"
              id="cardHolderName"
              name="cardHolderName"
              value={form.cardHolderName}
              onChange={handleInputChange}
              placeholder="John Doe"
              className="input"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="cardNumber">Card Number</label>
            <input
              type="text"
              id="cardNumber"
              name="cardNumber"
              value={form.cardNumber}
              onChange={handleInputChange}
              placeholder="4111 1111 1111 1111"
              className="input"
              maxLength={19}
              required
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="expiryDate">Expiry Date</label>
              <input
                type="text"
                id="expiryDate"
                name="expiryDate"
                value={form.expiryDate}
                onChange={handleInputChange}
                placeholder="MM/YY"
                className="input"
                maxLength={5}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="cvv">CVV</label>
              <input
                type="text"
                id="cvv"
                name="cvv"
                value={form.cvv}
                onChange={handleInputChange}
                placeholder="123"
                className="input"
                maxLength={4}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="carLicensePlate">Car License Plate <span className="optional-label">(optional – for parking)</span></label>
            <input
              type="text"
              id="carLicensePlate"
              name="carLicensePlate"
              value={form.carLicensePlate}
              onChange={handleInputChange}
              placeholder="CB1234AB"
              className="input"
              maxLength={10}
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-lg btn-full"
            disabled={loading}
          >
            {loading ? 'Processing...' : 'Confirm & Pay'}
          </button>
        </form>
      </div>
    </div>
  );
};
