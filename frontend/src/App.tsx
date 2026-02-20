import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { BookingProvider } from './contexts/BookingContext';
import { Navbar } from './components/Layout/Navbar';
import { Footer } from './components/Layout/Footer';

// Customer pages
import { CinemaSelectionPage } from './pages/customer/CinemaSelectionPage';
import { CinemaMoviesPage } from './pages/customer/CinemaMoviesPage';
import { CinemaMovieDetailPage } from './pages/customer/CinemaMovieDetailPage';
import { MoviesPage } from './pages/customer/MoviesPage';
import { MovieDetailPage } from './pages/customer/MovieDetailPage';
import { SeatSelectionPage } from './pages/customer/SeatSelectionPage';
import { CheckoutPage } from './pages/customer/CheckoutPage';
import { ConfirmationPage } from './pages/customer/ConfirmationPage';
import { MyBookingsPage } from './pages/customer/MyBookingsPage';

// Admin pages
import { DashboardPage } from './pages/admin/DashboardPage';
import { CinemasManagementPage } from './pages/admin/CinemasManagementPage';
import { MoviesManagementPage } from './pages/admin/MoviesManagementPage';
import { HallsManagementPage } from './pages/admin/HallsManagementPage';
import { ShowtimesManagementPage } from './pages/admin/ShowtimesManagementPage';
import { TicketTypesManagementPage } from './pages/admin/TicketTypesManagementPage';
import { LoyaltyManagementPage } from './pages/admin/LoyaltyManagementPage';
import { ReportsPage } from './pages/admin/ReportsPage';

// Auth pages
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { t } = useTranslation();
  const { isAuthenticated, loading } = useAuth();
  if (loading) return <div className="loading">{t('actions.loading')}</div>;
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
};

const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { t } = useTranslation();
  const { isAuthenticated, isAdmin, loading } = useAuth();
  if (loading) return <div className="loading">{t('actions.loading')}</div>;
  if (!isAuthenticated) return <Navigate to="/login" />;
  if (!isAdmin) return <Navigate to="/" />;
  return <>{children}</>;
};

const AppRoutes: React.FC = () => {
  return (
    <BrowserRouter>
      <Navbar />
      <main className="main-content">
        <Routes>
          {/* Public routes - only authentication pages */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected browsing routes */}
          <Route path="/" element={<ProtectedRoute><CinemaSelectionPage /></ProtectedRoute>} />
          <Route path="/cinemas/:cinemaId/movies" element={<ProtectedRoute><CinemaMoviesPage /></ProtectedRoute>} />
          <Route path="/cinemas/:cinemaId/movies/:movieId" element={<ProtectedRoute><CinemaMovieDetailPage /></ProtectedRoute>} />
          <Route path="/movies" element={<ProtectedRoute><MoviesPage /></ProtectedRoute>} />
          <Route path="/movies/:movieId" element={<ProtectedRoute><MovieDetailPage /></ProtectedRoute>} />

          {/* Protected customer routes */}
          <Route
            path="/showtime/:showtimeId/seats"
            element={
              <ProtectedRoute>
                <SeatSelectionPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/checkout/:reservationId"
            element={
              <ProtectedRoute>
                <CheckoutPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/confirmation/:bookingNumber"
            element={
              <ProtectedRoute>
                <ConfirmationPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/my-bookings"
            element={
              <ProtectedRoute>
                <MyBookingsPage />
              </ProtectedRoute>
            }
          />

          {/* Admin routes */}
          <Route
            path="/admin"
            element={
              <AdminRoute>
                <DashboardPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/cinemas"
            element={
              <AdminRoute>
                <CinemasManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/movies"
            element={
              <AdminRoute>
                <MoviesManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/halls"
            element={
              <AdminRoute>
                <HallsManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/showtimes"
            element={
              <AdminRoute>
                <ShowtimesManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/ticket-types"
            element={
              <AdminRoute>
                <TicketTypesManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/loyalty"
            element={
              <AdminRoute>
                <LoyaltyManagementPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/reports"
            element={
              <AdminRoute>
                <ReportsPage />
              </AdminRoute>
            }
          />

          {/* Fallback */}
          <Route path="*" element={<Navigate to="/" />} />
        </Routes>
      </main>
      <Footer />
    </BrowserRouter>
  );
};

const App: React.FC = () => {
  return (
    <AuthProvider>
      <BookingProvider>
        <AppRoutes />
      </BookingProvider>
    </AuthProvider>
  );
};

export default App;
