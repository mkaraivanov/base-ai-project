import apiClient from './apiClient';
import type { ApiResponse, LoyaltyCardDto, LoyaltySettingsDto, UpdateLoyaltySettingsDto } from '../types';

export const loyaltyApi = {
  getMyCard: async (): Promise<LoyaltyCardDto> => {
    const response = await apiClient.get<ApiResponse<LoyaltyCardDto>>('/loyalty/my-card');
    return response.data.data!;
  },

  getSettings: async (): Promise<LoyaltySettingsDto> => {
    const response = await apiClient.get<ApiResponse<LoyaltySettingsDto>>('/loyalty/settings');
    return response.data.data!;
  },

  updateSettings: async (data: UpdateLoyaltySettingsDto): Promise<LoyaltySettingsDto> => {
    const response = await apiClient.put<ApiResponse<LoyaltySettingsDto>>('/loyalty/settings', data);
    return response.data.data!;
  },
};
