import React, { useCallback, useEffect, useState } from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { DateRangePicker } from '../../../components/Reports/DateRangePicker';
import { GranularitySelector } from '../../../components/Reports/GranularitySelector';
import { ExportButton } from '../../../components/Reports/ExportButton';
import { useReportingData } from '../../../hooks/useReportingData';
import { reportingApi } from '../../../api/reportingApi';
import type { ReportGranularity, SalesByDateDto } from '../../types/reporting';

interface Props {
  readonly defaultFrom: string;
  readonly defaultTo: string;
}

export const SalesByDateTab: React.FC<Props> = ({ defaultFrom, defaultTo }) => {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultTo);
  const [granularity, setGranularity] = useState<ReportGranularity>('Daily');
  const [compare, setCompare] = useState(false);

  const fetchFn = useCallback(
    (q: { from: string; to: string; granularity?: ReportGranularity; compare?: boolean }) =>
      reportingApi.getSalesByDate(q),
    [],
  );

  const { data, status, error, load, exportCsv } = useReportingData<SalesByDateDto>(fetchFn);

  useEffect(() => {
    void load({ from, to, granularity, compare });
  }, [from, to, granularity, compare, load]);

  const handleDateChange = (newFrom: string, newTo: string) => {
    setFrom(newFrom);
    setTo(newTo);
  };

  const handleExport = () => exportCsv('date', { from, to, granularity, compare });

  return (
    <div className="report-tab">
      <div className="report-controls">
        <DateRangePicker from={from} to={to} onChange={handleDateChange} />
        <GranularitySelector value={granularity} onChange={setGranularity} />
        <label className="yoy-toggle">
          <input
            type="checkbox"
            checked={compare}
            onChange={(e) => setCompare(e.target.checked)}
          />
          Year-over-Year comparison
        </label>
        <ExportButton reportType="date" onExport={handleExport} />
      </div>

      {status === 'loading' && <p className="report-loading">Loading…</p>}
      {status === 'error' && <p className="report-error">{error}</p>}

      {status === 'success' && data.length > 0 && (
        <>
          <ResponsiveContainer width="100%" height={320}>
            <BarChart data={[...data]} margin={{ top: 10, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="period" tick={{ fontSize: 11 }} />
              <YAxis yAxisId="tickets" orientation="left" />
              <YAxis yAxisId="revenue" orientation="right" />
              <Tooltip formatter={(v: number, name: string) =>
                name.toLowerCase().includes('revenue') ? `$${v.toFixed(2)}` : v
              } />
              <Legend />
              <Bar yAxisId="tickets" dataKey="ticketsSold" name="Tickets Sold" fill="#4f8ef7" />
              {compare && (
                <Bar yAxisId="tickets" dataKey="compareTicketsSold" name="Tickets (Prior Year)" fill="#a8c8fa" />
              )}
              <Bar yAxisId="revenue" dataKey="revenue" name="Revenue ($)" fill="#22c55e" />
              {compare && (
                <Bar yAxisId="revenue" dataKey="compareRevenue" name="Revenue Prior Year ($)" fill="#86efac" />
              )}
            </BarChart>
          </ResponsiveContainer>

          <table className="report-table">
            <thead>
              <tr>
                <th>Period</th>
                <th>Tickets Sold</th>
                <th>Revenue</th>
                {compare && <><th>Prior Tickets</th><th>Prior Revenue</th></>}
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.period}>
                  <td>{row.period}</td>
                  <td>{row.ticketsSold}</td>
                  <td>${row.revenue.toFixed(2)}</td>
                  {compare && (
                    <>
                      <td>{row.compareTicketsSold ?? '–'}</td>
                      <td>{row.compareRevenue != null ? `$${row.compareRevenue.toFixed(2)}` : '–'}</td>
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}

      {status === 'success' && data.length === 0 && (
        <p className="report-empty">No data for the selected range.</p>
      )}
    </div>
  );
};
