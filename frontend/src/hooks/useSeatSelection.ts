import { useState, useCallback } from 'react';

const MAX_SEATS = 10;

interface UseSeatSelectionReturn {
  readonly selectedSeats: readonly string[];
  readonly toggleSeat: (seatNumber: string) => void;
  readonly clearSelection: () => void;
  readonly isSelected: (seatNumber: string) => boolean;
  readonly canSelectMore: boolean;
}

export const useSeatSelection = (): UseSeatSelectionReturn => {
  const [selectedSeats, setSelectedSeats] = useState<readonly string[]>([]);

  const toggleSeat = useCallback((seatNumber: string) => {
    setSelectedSeats((prev) => {
      if (prev.includes(seatNumber)) {
        return prev.filter((s) => s !== seatNumber);
      }
      if (prev.length >= MAX_SEATS) {
        return prev;
      }
      return [...prev, seatNumber];
    });
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedSeats([]);
  }, []);

  const isSelected = useCallback(
    (seatNumber: string) => selectedSeats.includes(seatNumber),
    [selectedSeats],
  );

  return {
    selectedSeats,
    toggleSeat,
    clearSelection,
    isSelected,
    canSelectMore: selectedSeats.length < MAX_SEATS,
  };
};
