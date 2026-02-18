// API Response envelope
export interface ApiResponse<T> {
  readonly success: boolean;
  readonly data: T | null;
  readonly error: string | null;
  readonly errors?: readonly string[];
}

// Cinemas
export interface CinemaDto {
  readonly id: string;
  readonly name: string;
  readonly address: string;
  readonly city: string;
  readonly country: string;
  readonly phoneNumber: string | null;
  readonly email: string | null;
  readonly logoUrl: string | null;
  readonly openTime: string;
  readonly closeTime: string;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly hallCount: number;
}

export interface CreateCinemaDto {
  readonly name: string;
  readonly address: string;
  readonly city: string;
  readonly country: string;
  readonly phoneNumber: string | null;
  readonly email: string | null;
  readonly logoUrl: string | null;
  readonly openTime: string;
  readonly closeTime: string;
}

export interface UpdateCinemaDto {
  readonly name: string;
  readonly address: string;
  readonly city: string;
  readonly country: string;
  readonly phoneNumber: string | null;
  readonly email: string | null;
  readonly logoUrl: string | null;
  readonly openTime: string;
  readonly closeTime: string;
  readonly isActive: boolean;
}

// Auth
export interface LoginDto {
  readonly email: string;
  readonly password: string;
}

export interface RegisterDto {
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly phoneNumber: string;
}

export interface AuthResponseDto {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly role: string;
  readonly token: string;
  readonly expiresAt: string;
}

export interface User {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly role: string;
}

// Movies
export interface MovieDto {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly genre: string;
  readonly durationMinutes: number;
  readonly rating: string;
  readonly posterUrl: string;
  readonly releaseDate: string;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface CreateMovieDto {
  readonly title: string;
  readonly description: string;
  readonly genre: string;
  readonly durationMinutes: number;
  readonly rating: string;
  readonly posterUrl: string;
  readonly releaseDate: string;
}

export interface UpdateMovieDto {
  readonly title: string;
  readonly description: string;
  readonly genre: string;
  readonly durationMinutes: number;
  readonly rating: string;
  readonly posterUrl: string;
  readonly releaseDate: string;
  readonly isActive: boolean;
}

// Cinema Halls
export interface SeatDefinition {
  readonly seatNumber: string;
  readonly row: number;
  readonly column: number;
  readonly seatType: string;
  readonly priceMultiplier: number;
  readonly isAvailable: boolean;
}

export interface SeatLayout {
  readonly rows: number;
  readonly seatsPerRow: number;
  readonly seats: readonly SeatDefinition[];
}

export interface CinemaHallDto {
  readonly id: string;
  readonly cinemaId: string;
  readonly cinemaName: string;
  readonly name: string;
  readonly totalSeats: number;
  readonly seatLayout: SeatLayout;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface CreateCinemaHallDto {
  readonly cinemaId: string;
  readonly name: string;
  readonly seatLayout: SeatLayout;
}

export interface UpdateCinemaHallDto {
  readonly name: string;
  readonly seatLayout: SeatLayout;
  readonly isActive: boolean;
}

// Showtimes
export interface ShowtimeDto {
  readonly id: string;
  readonly movieId: string;
  readonly movieTitle: string;
  readonly cinemaHallId: string;
  readonly hallName: string;
  readonly cinemaId: string;
  readonly cinemaName: string;
  readonly startTime: string;
  readonly endTime: string;
  readonly basePrice: number;
  readonly availableSeats: number;
  readonly isActive: boolean;
}

export interface CreateShowtimeDto {
  readonly movieId: string;
  readonly cinemaHallId: string;
  readonly startTime: string;
  readonly basePrice: number;
}

// Bookings / Seats
export interface SeatDto {
  readonly seatNumber: string;
  readonly seatType: string;
  readonly price: number;
  readonly status: 'Available' | 'Reserved' | 'Booked' | 'Blocked';
}

export interface SeatAvailabilityDto {
  readonly showtimeId: string;
  readonly availableSeats: readonly SeatDto[];
  readonly reservedSeats: readonly SeatDto[];
  readonly bookedSeats: readonly SeatDto[];
  readonly totalSeats: number;
}

export interface CreateReservationDto {
  readonly showtimeId: string;
  readonly seatNumbers: readonly string[];
}

export interface ReservationDto {
  readonly id: string;
  readonly showtimeId: string;
  readonly seatNumbers: readonly string[];
  readonly totalAmount: number;
  readonly expiresAt: string;
  readonly status: string;
  readonly createdAt: string;
}

export interface ConfirmBookingDto {
  readonly reservationId: string;
  readonly paymentMethod: string;
  readonly cardNumber: string;
  readonly cardHolderName: string;
  readonly expiryDate: string;
  readonly cvv: string;
  readonly carLicensePlate?: string;
}

export interface BookingDto {
  readonly id: string;
  readonly bookingNumber: string;
  readonly showtimeId: string;
  readonly movieTitle: string;
  readonly showtimeStart: string;
  readonly hallName: string;
  readonly seatNumbers: readonly string[];
  readonly totalAmount: number;
  readonly status: string;
  readonly bookedAt: string;
  readonly carLicensePlate?: string;
}
