import React from 'react';
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { Toaster } from 'sonner';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { BookingProvider } from './contexts/BookingContext';
import { Navbar } from './components/Layout/Navbar';
import { Footer } from './components/Layout/Footer';
import { BottomTabBar } from './components/Layout/BottomTabBar';

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

const pageVariants = {
  initial: { opacity: 0, y: 6 },
  enter: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -6 },
};

const pageTransition = { duration: 0.18, ease: 'easeInOut' };

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();
  if (loading) return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
      <CircularProgress />
    </Box>
  );
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
};

const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isAdmin, loading } = useAuth();
  if (loading) return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
      <CircularProgress />
    </Box>
  );
  if (!isAuthenticated) return <Navigate to="/login" />;
  if (!isAdmin) return <Navigate to="/" />;
  return <>{children}</>;
};

function AnimatedRoutes() {
  const location = useLocation();
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={location.pathname}
        variants={pageVariants}
        initial="initial"
        animate="enter"
        exit="exit"
        transition={pageTransition}
        style={{ flex: 1, display: 'flex', flexDirection: 'column' }}
      >
        <Routes location={location}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          <Route path="/" element={<ProtectedRoute><CinemaSelectionPage /></ProtectedRoute>} />
          <Route path="/cinemas/:cinemaId/movies" element={<ProtectedRoute><CinemaMoviesPage /></ProtectedRoute>} />
          <Route path="/cinemas/:cinemaId/movies/:movieId" element={<ProtectedRoute><CinemaMovieDetailPage /></ProtectedRoute>} />
          <Route path="/movies" element={<ProtectedRoute><MoviesPage /></ProtectedRoute>} />
          <Route path="/movies/:movieId" element={<ProtectedRoute><MovieDetailPage /></ProtectedRoute>} />
          <Route path="/showtime/:showtimeId/seats" element={<ProtectedRoute><SeatSelectionPage /></ProtectedRoute>} />
          <Route path="/checkout/:reservationId" element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>} />
          <Route path="/confirmation/:bookingNumber" element={<ProtectedRoute><ConfirmationPage /></ProtectedRoute>} />
          <Route path="/my-bookings" element={<ProtectedRoute><MyBookingsPage /></ProtectedRoute>} />

          <Route path="/admin" element={<AdminRoute><DashboardPage /></AdminRoute>} />
          <Route path="/admin/cinemas" element={<AdminRoute><CinemasManagementPage /></AdminRoute>} />
          <Route path="/admin/movies" element={<AdminRoute><MoviesManagementPage /></AdminRoute>} />
          <Route path="/admin/halls" element={<AdminRoute><HallsManagementPage /></AdminRoute>} />
          <Route path="/admin/showtimes" element={<AdminRoute><ShowtimesManagementPage /></AdminRoute>} />
          <Route path="/admin/ticket-types" element={<AdminRoute><TicketTypesManagementPage /></AdminRoute>} />
          <Route path="/admin/loyalty" element={<AdminRoute><LoyaltyManagementPage /></AdminRoute>} />
          <Route path="/admin/reports" element={<AdminRoute><ReportsPage /></AdminRoute>} />

          <Route path="*" element={<Navigate to="/" />} />
        </Routes>
      </motion.div>
    </AnimatePresence>
  );
}

const AppRoutes: React.FC = () => {
  return (
    <BrowserRouter>
      <Navbar />
      <Box component="main" sx={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
        <AnimatedRoutes />
      </Box>
      <Footer />
      <BottomTabBar />
      <Toaster position="top-right" richColors />
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

