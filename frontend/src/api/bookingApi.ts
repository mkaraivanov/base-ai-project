import apiClient from './apiClient';
import type {
  ApiResponse,
  SeatAvailabilityDto,
  ReservationDto,
  BookingDto,
  ConfirmBookingDto,
} from '../types';

export const bookingApi = {
  getSeatAvailability: async (showtimeId: string): Promise<SeatAvailabilityDto> => {
    const response = await apiClient.get<ApiResponse<SeatAvailabilityDto>>(
      `/bookings/availability/${showtimeId}`,
    );
    return response.data.data!;
  },

  createReservation: async (
    showtimeId: string,
    seats: ReadonlyArray<{ seatNumber: string; ticketTypeId: string }>,
  ): Promise<ReservationDto> => {
    const response = await apiClient.post<ApiResponse<ReservationDto>>('/bookings/reserve', {
      showtimeId,
      seats,
    });
    return response.data.data!;
  },

  cancelReservation: async (reservationId: string): Promise<void> => {
    await apiClient.delete(`/bookings/reserve/${reservationId}`);
  },

  confirmBooking: async (data: ConfirmBookingDto): Promise<BookingDto> => {
    const response = await apiClient.post<ApiResponse<BookingDto>>('/bookings/confirm', data);
    return response.data.data!;
  },

  getMyBookings: async (): Promise<readonly BookingDto[]> => {
    const response = await apiClient.get<ApiResponse<BookingDto[]>>('/bookings/my-bookings');
    return response.data.data ?? [];
  },

  getByBookingNumber: async (bookingNumber: string): Promise<BookingDto> => {
    const response = await apiClient.get<ApiResponse<BookingDto>>(
      `/bookings/${bookingNumber}`,
    );
    return response.data.data!;
  },

  cancelBooking: async (bookingId: string): Promise<BookingDto> => {
    const response = await apiClient.post<ApiResponse<BookingDto>>(
      `/bookings/${bookingId}/cancel`,
    );
    return response.data.data!;
  },
};
