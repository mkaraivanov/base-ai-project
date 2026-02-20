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
import { ExportButton } from '../../../components/Reports/ExportButton';
import { useReportingData } from '../../../hooks/useReportingData';
import { reportingApi } from '../../../api/reportingApi';
import type { SalesByMovieDto } from '../../../types/reporting';

interface Props {
  readonly defaultFrom: string;
  readonly defaultTo: string;
}

export const SalesByMovieTab: React.FC<Props> = ({ defaultFrom, defaultTo }) => {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultTo);

  const fetchFn = useCallback(
    (q: { from: string; to: string }) => reportingApi.getSalesByMovie(q),
    [],
  );

  const { data, status, error, load, exportCsv } = useReportingData<SalesByMovieDto>(fetchFn);

  useEffect(() => {
    void load({ from, to });
  }, [from, to, load]);

  const handleDateChange = (newFrom: string, newTo: string) => {
    setFrom(newFrom);
    setTo(newTo);
  };

  const handleExport = () => exportCsv('movie', { from, to });

  return (
    <div className="report-tab">
      <div className="report-controls">
        <DateRangePicker from={from} to={to} onChange={handleDateChange} />
        <ExportButton reportType="movie" onExport={handleExport} />
      </div>

      {status === 'loading' && <p className="report-loading">Loading…</p>}
      {status === 'error' && <p className="report-error">{error}</p>}

      {status === 'success' && data.length > 0 && (
        <>
          <ResponsiveContainer width="100%" height={320}>
            <BarChart
              data={[...data].slice(0, 15)}
              layout="vertical"
              margin={{ top: 5, right: 30, left: 100, bottom: 5 }}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis type="number" />
              <YAxis dataKey="movieTitle" type="category" tick={{ fontSize: 11 }} width={95} />
              <Tooltip formatter={(v: number, name: string) =>
                name.toLowerCase().includes('revenue') ? `$${v.toFixed(2)}` : v
              } />
              <Legend />
              <Bar dataKey="ticketsSold" name="Tickets Sold" fill="#4f8ef7" />
              <Bar dataKey="revenue" name="Revenue ($)" fill="#22c55e" />
            </BarChart>
          </ResponsiveContainer>

          <table className="report-table">
            <thead>
              <tr>
                <th>Movie</th>
                <th>Tickets Sold</th>
                <th>Revenue</th>
                <th>Capacity Used</th>
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.movieId}>
                  <td>{row.movieTitle}</td>
                  <td>{row.ticketsSold}</td>
                  <td>${row.revenue.toFixed(2)}</td>
                  <td>{row.capacityUsedPercent.toFixed(1)}%</td>
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
