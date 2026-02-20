import React, { useEffect, useState, useCallback } from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface BookingTimerProps {
  readonly expiresAt: Date;
  readonly onExpire: () => void;
}

export const BookingTimer: React.FC<BookingTimerProps> = ({ expiresAt, onExpire }) => {
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

  const timerColor =
    timeLeft <= 60 ? 'error.main' :
    timeLeft <= 120 ? 'warning.main' :
    'text.secondary';

  const pulseAnim = timeLeft <= 60 ? {
    animation: 'pulse 1s ease-in-out infinite',
  } : {};

  return (
    <Box
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 1,
        color: timerColor,
        ...pulseAnim,
      }}
    >
      <span>⏱️</span>
      <Typography variant="body2" fontWeight={600} color="inherit">
        Time remaining: {minutes}:{seconds.toString().padStart(2, '0')}
      </Typography>
    </Box>
  );
};
