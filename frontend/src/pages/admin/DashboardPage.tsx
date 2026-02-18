import React from 'react';
import { Link } from 'react-router-dom';

export const DashboardPage: React.FC = () => {
  return (
    <div className="page">
      <div className="container">
        <h1>Admin Dashboard</h1>

        <div className="admin-grid">
          <Link to="/admin/cinemas" className="admin-card">
            <div className="admin-card-icon">🏛️</div>
            <h3>Cinemas</h3>
            <p>Manage cinemas, locations and opening hours</p>
          </Link>

          <Link to="/admin/halls" className="admin-card">
            <div className="admin-card-icon">🎟️</div>
            <h3>Cinema Halls</h3>
            <p>Manage halls, configure seat layouts</p>
          </Link>

          <Link to="/admin/movies" className="admin-card">
            <div className="admin-card-icon">🎬</div>
            <h3>Movies</h3>
            <p>Manage movies, add new titles, update details</p>
          </Link>

          <Link to="/admin/showtimes" className="admin-card">
            <div className="admin-card-icon">🕐</div>
            <h3>Showtimes</h3>
            <p>Schedule and manage movie showtimes</p>
          </Link>

          <Link to="/admin/ticket-types" className="admin-card">
            <div className="admin-card-icon">🎟️</div>
            <h3>Ticket Types</h3>
            <p>Manage ticket types and price modifiers (Adult, Children, Senior, etc.)</p>
          </Link>
        </div>
      </div>
    </div>
  );
};
