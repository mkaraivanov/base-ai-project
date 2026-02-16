import React from 'react';
import type { SeatDto } from '../../types';
import './SeatMap.css';

interface SeatMapProps {
  readonly seats: readonly SeatDto[];
  readonly selectedSeats: readonly string[];
  readonly onSeatClick: (seatNumber: string) => void;
}

export const SeatMap: React.FC<SeatMapProps> = ({ seats, selectedSeats, onSeatClick }) => {
  const seatsByRow = seats.reduce<Record<string, SeatDto[]>>((acc, seat) => {
    const row = seat.seatNumber[0];
    return {
      ...acc,
      [row]: [...(acc[row] ?? []), seat],
    };
  }, {});

  const getSeatClassName = (seat: SeatDto): string => {
    const base = 'seat';
    if (seat.status === 'Booked') return `${base} seat-booked`;
    if (seat.status === 'Reserved') return `${base} seat-reserved`;
    if (selectedSeats.includes(seat.seatNumber)) return `${base} seat-selected`;
    return `${base} seat-available`;
  };

  const handleSeatClick = (seat: SeatDto) => {
    if (seat.status === 'Available' || selectedSeats.includes(seat.seatNumber)) {
      onSeatClick(seat.seatNumber);
    }
  };

  return (
    <div className="seat-map">
      <div className="screen">SCREEN</div>

      <div className="seats-container">
        {Object.entries(seatsByRow)
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([row, rowSeats]) => (
            <div key={row} className="seat-row">
              <span className="row-label">{row}</span>
              <div className="seats">
                {[...rowSeats]
                  .sort((a, b) => {
                    const numA = parseInt(a.seatNumber.slice(1), 10);
                    const numB = parseInt(b.seatNumber.slice(1), 10);
                    return numA - numB;
                  })
                  .map((seat) => (
                    <button
                      key={seat.seatNumber}
                      className={getSeatClassName(seat)}
                      onClick={() => handleSeatClick(seat)}
                      disabled={
                        seat.status !== 'Available' &&
                        !selectedSeats.includes(seat.seatNumber)
                      }
                      title={`${seat.seatNumber} - $${seat.price.toFixed(2)}`}
                    >
                      {seat.seatNumber}
                    </button>
                  ))}
              </div>
            </div>
          ))}
      </div>

      <div className="legend">
        <div className="legend-item">
          <div className="seat seat-available legend-seat" />
          <span>Available</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-selected legend-seat" />
          <span>Selected</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-reserved legend-seat" />
          <span>Reserved</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-booked legend-seat" />
          <span>Booked</span>
        </div>
      </div>
    </div>
  );
};
