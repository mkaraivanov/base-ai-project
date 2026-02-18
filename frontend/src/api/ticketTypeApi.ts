import apiClient from './apiClient';
import type {
  ApiResponse,
  TicketTypeDto,
  CreateTicketTypeDto,
  UpdateTicketTypeDto,
} from '../types';

export const ticketTypeApi = {
  /** Public: returns only active ticket types (for customer seat selection) */
  getActive: async (): Promise<readonly TicketTypeDto[]> => {
    const response = await apiClient.get<ApiResponse<TicketTypeDto[]>>('/ticket-types');
    return response.data.data ?? [];
  },

  /** Admin: returns all ticket types including inactive */
  getAll: async (): Promise<readonly TicketTypeDto[]> => {
    const response = await apiClient.get<ApiResponse<TicketTypeDto[]>>('/ticket-types/all');
    return response.data.data ?? [];
  },

  create: async (data: CreateTicketTypeDto): Promise<TicketTypeDto> => {
    const response = await apiClient.post<ApiResponse<TicketTypeDto>>('/ticket-types', data);
    return response.data.data!;
  },

  update: async (id: string, data: UpdateTicketTypeDto): Promise<TicketTypeDto> => {
    const response = await apiClient.put<ApiResponse<TicketTypeDto>>(`/ticket-types/${id}`, data);
    return response.data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/ticket-types/${id}`);
  },
};
