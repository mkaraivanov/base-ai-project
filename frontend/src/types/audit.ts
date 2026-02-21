export interface AuditLogDto {
  readonly id: string;
  readonly entityName: string;
  readonly entityId: string;
  readonly action: string;
  readonly userId: string | null;
  readonly userEmail: string | null;
  readonly userRole: string | null;
  readonly ipAddress: string | null;
  readonly oldValues: string | null;
  readonly newValues: string | null;
  readonly timestamp: string;
}

export interface AuditLogFilterParams {
  readonly dateFrom?: string;
  readonly dateTo?: string;
  readonly userId?: string;
  readonly action?: string;
  readonly entityName?: string;
  readonly search?: string;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
  readonly hasNextPage: boolean;
  readonly hasPreviousPage: boolean;
}
