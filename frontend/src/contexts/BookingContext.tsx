import React, { createContext, useContext, useState, useCallback } from 'react';
import type { ReservationDto } from '../types';

interface BookingContextType {
  readonly reservation: ReservationDto | null;
  readonly selectedSeats: readonly string[];
  readonly setReservation: (reservation: ReservationDto | null) => void;
  readonly setSelectedSeats: React.Dispatch<React.SetStateAction<readonly string[]>>;
  readonly clearBooking: () => void;
}

const BookingContext = createContext<BookingContextType | undefined>(undefined);

export const BookingProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [reservation, setReservation] = useState<ReservationDto | null>(null);
  const [selectedSeats, setSelectedSeats] = useState<readonly string[]>([]);

  const clearBooking = useCallback(() => {
    setReservation(null);
    setSelectedSeats([]);
  }, []);

  const value: BookingContextType = {
    reservation,
    selectedSeats,
    setReservation,
    setSelectedSeats,
    clearBooking,
  };

  return <BookingContext.Provider value={value}>{children}</BookingContext.Provider>;
};

export const useBooking = (): BookingContextType => {
  const context = useContext(BookingContext);
  if (!context) {
    throw new Error('useBooking must be used within BookingProvider');
  }
  return context;
};
