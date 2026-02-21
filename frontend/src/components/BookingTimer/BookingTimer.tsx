import React, { useEffect, useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';

interface BookingTimerProps {
  readonly expiresAt: Date;
  readonly onExpire: () => void;
}

export const BookingTimer: React.FC<BookingTimerProps> = ({ expiresAt, onExpire }) => {
  const { t } = useTranslation('customer');
  const calculateTimeLeft = useCallback((): number => {
    const now = Date.now();
    const expiry = new Date(expiresAt).getTime();
    return Math.max(0, Math.floor((expiry - now) / 1000));
  }, [expiresAt]);

  const [timeLeft, setTimeLeft] = useState<number>(calculateTimeLeft);

  useEffect(() => {
    setTimeLeft(calculateTimeLeft());

    const interval = setInterval(() => {
      const remaining = calculateTimeLeft();
      setTimeLeft(remaining);

      if (remaining === 0) {
        clearInterval(interval);
        onExpire();
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [expiresAt, onExpire, calculateTimeLeft]);

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  const getTimerClassName = (): string => {
    if (timeLeft <= 60) return 'booking-timer timer-critical';
    if (timeLeft <= 120) return 'booking-timer timer-warning';
    return 'booking-timer timer-normal';
  };

  return (
    <div className={getTimerClassName()}>
      <span className="timer-icon">⏱️</span>
      <span className="timer-text">
        {t('seatSelection.timeRemaining', { minutes, seconds: seconds.toString().padStart(2, '0') })}
      </span>
    </div>
  );
};
