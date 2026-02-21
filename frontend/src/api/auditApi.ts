import apiClient from './apiClient';
import type { ApiResponse } from '../types';
import type { AuditLogDto, AuditLogFilterParams, PagedResult } from '../types/audit';

export const auditApi = {
  getAuditLogs: async (
    filter: AuditLogFilterParams,
    page = 1,
    pageSize = 20,
  ): Promise<PagedResult<AuditLogDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<AuditLogDto>>>('/audit', {
      params: {
        ...(filter.dateFrom && { dateFrom: filter.dateFrom }),
        ...(filter.dateTo && { dateTo: filter.dateTo }),
        ...(filter.userId && { userId: filter.userId }),
        ...(filter.action && { action: filter.action }),
        ...(filter.entityName && { entityName: filter.entityName }),
        ...(filter.search && { search: filter.search }),
        page,
        pageSize,
      },
    });
    return response.data.data ?? { items: [], totalCount: 0, page: 1, pageSize, totalPages: 0, hasNextPage: false, hasPreviousPage: false };
  },

  getAuditLogById: async (id: string): Promise<AuditLogDto> => {
    const response = await apiClient.get<ApiResponse<AuditLogDto>>(`/audit/${id}`);
    if (!response.data.data) throw new Error(response.data.error ?? 'Not found');
    return response.data.data;
  },

  exportAuditLogsCsv: async (filter: AuditLogFilterParams): Promise<void> => {
    const response = await apiClient.get('/audit/export', {
      params: {
        ...(filter.dateFrom && { dateFrom: filter.dateFrom }),
        ...(filter.dateTo && { dateTo: filter.dateTo }),
        ...(filter.userId && { userId: filter.userId }),
        ...(filter.action && { action: filter.action }),
        ...(filter.entityName && { entityName: filter.entityName }),
        ...(filter.search && { search: filter.search }),
      },
      responseType: 'blob',
    });
    const blob = new Blob([response.data as BlobPart], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `audit-log-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  },
};
