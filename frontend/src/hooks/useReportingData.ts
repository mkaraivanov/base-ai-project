import { useState, useCallback } from 'react';
import { reportingApi } from '../api/reportingApi';
import type { ReportQueryParams } from '../types/reporting';

type Status = 'idle' | 'loading' | 'success' | 'error';

interface UseReportingDataResult<T> {
  readonly data: readonly T[];
  readonly status: Status;
  readonly error: string | null;
  readonly load: (query: ReportQueryParams) => Promise<void>;
  readonly exportCsv: (reportType: string, query: ReportQueryParams) => Promise<void>;
}

type FetchFn<T> = (query: ReportQueryParams) => Promise<readonly T[]>;

export function useReportingData<T>(fetchFn: FetchFn<T>): UseReportingDataResult<T> {
  const [data, setData] = useState<readonly T[]>([]);
  const [status, setStatus] = useState<Status>('idle');
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    async (query: ReportQueryParams) => {
      setStatus('loading');
      setError(null);
      try {
        const result = await fetchFn(query);
        setData(result);
        setStatus('success');
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load report';
        setError(message);
        setStatus('error');
      }
    },
    [fetchFn],
  );

  const exportCsv = useCallback(async (reportType: string, query: ReportQueryParams) => {
    try {
      await reportingApi.exportCsv(reportType, query);
    } catch {
      // Silently ignore export errors — could add toast notification here
    }
  }, []);

  return { data, status, error, load, exportCsv };
}
