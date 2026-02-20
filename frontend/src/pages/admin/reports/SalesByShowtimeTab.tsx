import React, { useCallback, useEffect, useState } from 'react';
import { DateRangePicker } from '../../../components/Reports/DateRangePicker';
import { useReportingData } from '../../../hooks/useReportingData';
import { reportingApi } from '../../../api/reportingApi';
import type { SalesByShowtimeDto } from '../../../types/reporting';

interface Props {
  readonly defaultFrom: string;
  readonly defaultTo: string;
}

export const SalesByShowtimeTab: React.FC<Props> = ({ defaultFrom, defaultTo }) => {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultTo);
  const [cinemaFilter, setCinemaFilter] = useState('');
  const [movieFilter, setMovieFilter] = useState('');

  const fetchFn = useCallback(
    (q: { from: string; to: string; cinemaId?: string; movieId?: string }) =>
      reportingApi.getSalesByShowtime(q),
    [],
  );

  const { data, status, error, load } = useReportingData<SalesByShowtimeDto>(fetchFn);

  useEffect(() => {
    void load({
      from,
      to,
      ...(cinemaFilter && { cinemaId: cinemaFilter }),
      ...(movieFilter && { movieId: movieFilter }),
    });
  }, [from, to, cinemaFilter, movieFilter, load]);

  const handleDateChange = (newFrom: string, newTo: string) => {
    setFrom(newFrom);
    setTo(newTo);
  };

  return (
    <div className="report-tab">
      <div className="report-controls">
        <DateRangePicker from={from} to={to} onChange={handleDateChange} />
        <label className="filter-label">
          Cinema ID (optional)
          <input
            type="text"
            value={cinemaFilter}
            onChange={(e) => setCinemaFilter(e.target.value)}
            placeholder="Cinema UUID"
            className="filter-input"
          />
        </label>
        <label className="filter-label">
          Movie ID (optional)
          <input
            type="text"
            value={movieFilter}
            onChange={(e) => setMovieFilter(e.target.value)}
            placeholder="Movie UUID"
            className="filter-input"
          />
        </label>
      </div>

      {status === 'loading' && <p className="report-loading">Loading…</p>}
      {status === 'error' && <p className="report-error">{error}</p>}

      {status === 'success' && data.length > 0 && (
        <table className="report-table">
          <thead>
            <tr>
              <th>Start Time</th>
              <th>Movie</th>
              <th>Hall</th>
              <th>Cinema</th>
              <th>Tickets Sold</th>
              <th>Capacity</th>
              <th>Occupancy</th>
              <th>Revenue</th>
            </tr>
          </thead>
          <tbody>
            {data.map((row) => (
              <tr key={row.showtimeId}>
                <td>{new Date(row.startTime).toLocaleString()}</td>
                <td>{row.movieTitle}</td>
                <td>{row.hallName}</td>
                <td>{row.cinemaName}</td>
                <td>{row.ticketsSold}</td>
                <td>{row.capacity}</td>
                <td>{row.occupancyPercent.toFixed(1)}%</td>
                <td>${row.revenue.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {status === 'success' && data.length === 0 && (
        <p className="report-empty">No data for the selected range.</p>
      )}
    </div>
  );
};
