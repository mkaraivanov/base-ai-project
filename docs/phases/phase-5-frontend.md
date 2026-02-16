# Phase 5: Frontend Development

**Duration:** Week 4
**Status:** 🔵 Pending

## Overview

This phase builds the React 19.2 frontend application with Vite. It implements the customer booking flow, admin dashboards, and integrates with all backend APIs. The frontend includes an interactive seat selection interface, real-time seat availability, and booking timer.

## Objectives

✅ Set up React 19.2 + Vite + TypeScript project
✅ Configure React Router for navigation
✅ Implement authentication (Context API)
✅ Create customer pages (Movies, Seat Selection, Checkout, Confirmation)
✅ Create admin pages (Movies, Halls, Showtimes, Reports)
✅ Build interactive seat map component
✅ Implement booking timer countdown
✅ Integrate with backend APIs
✅ Add responsive design
✅ Achieve 80%+ test coverage on business logic

---

## Step 1: Frontend Project Setup

### 1.1 Create React + Vite Project

```bash
cd /Users/martin.karaivanov/Projects/base-ai-project

# The frontend directory already exists, so just install dependencies
cd frontend
npm install
```

### 1.2 Install Required Packages

```bash
# Routing
npm install react-router-dom

# HTTP Client
npm install axios

# Date formatting
npm install date-fns

# State management (optional, using Context API)
# npm install zustand

# UI Components (optional)
# npm install @headlessui/react @heroicons/react

# Testing
npm install --save-dev @testing-library/react @testing-library/jest-dom @testing-library/user-event vitest jsdom
```

### 1.3 Configure TypeScript

**File:** `frontend/tsconfig.json`

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

---

## Step 2: Project Structure

Create the following directory structure:

```
frontend/
├── src/
│   ├── api/              # API client and endpoints
│   │   ├── apiClient.ts
│   │   ├── authApi.ts
│   │   ├── movieApi.ts
│   │   ├── showtimeApi.ts
│   │   └── bookingApi.ts
│   ├── components/       # Reusable components
│   │   ├── SeatMap/
│   │   │   ├── SeatMap.tsx
│   │   │   ├── Seat.tsx
│   │   │   └── SeatMap.css
│   │   ├── BookingTimer/
│   │   │   └── BookingTimer.tsx
│   │   ├── MovieCard/
│   │   │   └── MovieCard.tsx
│   │   └── Layout/
│   │       ├── Navbar.tsx
│   │       └── Footer.tsx
│   ├── contexts/         # React Context
│   │   ├── AuthContext.tsx
│   │   └── BookingContext.tsx
│   ├── hooks/            # Custom hooks
│   │   ├── useSeatSelection.ts
│   │   └── useAuth.ts
│   ├── pages/            # Page components
│   │   ├── customer/
│   │   │   ├── HomePage.tsx
│   │   │   ├── MoviesPage.tsx
│   │   │   ├── MovieDetailPage.tsx
│   │   │   ├── SeatSelectionPage.tsx
│   │   │   ├── CheckoutPage.tsx
│   │   │   ├── ConfirmationPage.tsx
│   │   │   └── MyBookingsPage.tsx
│   │   ├── admin/
│   │   │   ├── DashboardPage.tsx
│   │   │   ├── MoviesManagementPage.tsx
│   │   │   ├── HallsManagementPage.tsx
│   │   │   └── ShowtimesManagementPage.tsx
│   │   └── auth/
│   │       ├── LoginPage.tsx
│   │       └── RegisterPage.tsx
│   ├── types/            # TypeScript types
│   │   └── index.ts
│   ├── utils/            # Utility functions
│   │   └── formatters.ts
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
```

---

## Step 3: API Client Setup

### 3.1 Create API Client

**File:** `frontend/src/api/apiClient.ts`

```typescript
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Clear token and redirect to login
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### 3.2 Create API Endpoints

**File:** `frontend/src/api/bookingApi.ts`

```typescript
import apiClient from './apiClient';
import type { ApiResponse, SeatAvailability, Reservation, Booking } from '../types';

export const bookingApi = {
  getSeatAvailability: async (showtimeId: string): Promise<SeatAvailability> => {
    const response = await apiClient.get<ApiResponse<SeatAvailability>>(
      `/bookings/availability/${showtimeId}`
    );
    return response.data.data!;
  },

  createReservation: async (showtimeId: string, seatNumbers: string[]): Promise<Reservation> => {
    const response = await apiClient.post<ApiResponse<Reservation>>('/bookings/reserve', {
      showtimeId,
      seatNumbers,
    });
    return response.data.data!;
  },

  cancelReservation: async (reservationId: string): Promise<void> => {
    await apiClient.delete(`/bookings/reserve/${reservationId}`);
  },

  confirmBooking: async (reservationId: string, paymentData: any): Promise<Booking> => {
    const response = await apiClient.post<ApiResponse<Booking>>('/bookings/confirm', {
      reservationId,
      ...paymentData,
    });
    return response.data.data!;
  },

  getMyBookings: async (): Promise<Booking[]> => {
    const response = await apiClient.get<ApiResponse<Booking[]>>('/bookings/my-bookings');
    return response.data.data!;
  },

  cancelBooking: async (bookingId: string): Promise<void> => {
    await apiClient.post(`/bookings/${bookingId}/cancel`);
  },
};
```

---

## Step 4: Authentication Context

**File:** `frontend/src/contexts/AuthContext.tsx`

```typescript
import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi } from '../api/authApi';
import type { User } from '../types';

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    // Load token from localStorage on mount
    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('authUser');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }
  }, []);

  const login = async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    setToken(response.token);
    setUser({
      userId: response.userId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role,
    });

    localStorage.setItem('authToken', response.token);
    localStorage.setItem('authUser', JSON.stringify({
      userId: response.userId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role,
    }));
  };

  const register = async (data: RegisterData) => {
    const response = await authApi.register(data);
    // Auto-login after registration
    await login(data.email, data.password);
  };

  const logout = () => {
    setUser(null);
    setToken(null);
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');
  };

  const value = {
    user,
    token,
    login,
    register,
    logout,
    isAuthenticated: !!token,
    isAdmin: user?.role === 'Admin',
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

---

## Step 5: Key Components

### 5.1 Seat Map Component

**File:** `frontend/src/components/SeatMap/SeatMap.tsx`

```typescript
import React from 'react';
import type { SeatDto } from '../../types';
import './SeatMap.css';

interface SeatMapProps {
  seats: SeatDto[];
  selectedSeats: string[];
  onSeatClick: (seatNumber: string) => void;
}

export const SeatMap: React.FC<SeatMapProps> = ({ seats, selectedSeats, onSeatClick }) => {
  // Group seats by row
  const seatsByRow = seats.reduce((acc, seat) => {
    const row = seat.seatNumber[0];
    if (!acc[row]) {
      acc[row] = [];
    }
    acc[row].push(seat);
    return acc;
  }, {} as Record<string, SeatDto[]>);

  const getSeatClassName = (seat: SeatDto): string => {
    const baseClass = 'seat';
    if (seat.status === 'Booked') return `${baseClass} seat-booked`;
    if (seat.status === 'Reserved') return `${baseClass} seat-reserved`;
    if (selectedSeats.includes(seat.seatNumber)) return `${baseClass} seat-selected`;
    return `${baseClass} seat-available`;
  };

  const handleSeatClick = (seat: SeatDto) => {
    if (seat.status === 'Available' || selectedSeats.includes(seat.seatNumber)) {
      onSeatClick(seat.seatNumber);
    }
  };

  return (
    <div className="seat-map">
      <div className="screen">SCREEN</div>

      <div className="seats-container">
        {Object.entries(seatsByRow)
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([row, rowSeats]) => (
            <div key={row} className="seat-row">
              <span className="row-label">{row}</span>
              <div className="seats">
                {rowSeats
                  .sort((a, b) => {
                    const numA = parseInt(a.seatNumber.slice(1));
                    const numB = parseInt(b.seatNumber.slice(1));
                    return numA - numB;
                  })
                  .map((seat) => (
                    <button
                      key={seat.seatNumber}
                      className={getSeatClassName(seat)}
                      onClick={() => handleSeatClick(seat)}
                      disabled={seat.status !== 'Available' && !selectedSeats.includes(seat.seatNumber)}
                      title={`${seat.seatNumber} - $${seat.price.toFixed(2)}`}
                    >
                      {seat.seatNumber}
                    </button>
                  ))}
              </div>
            </div>
          ))}
      </div>

      <div className="legend">
        <div className="legend-item">
          <div className="seat seat-available"></div>
          <span>Available</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-selected"></div>
          <span>Selected</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-reserved"></div>
          <span>Reserved</span>
        </div>
        <div className="legend-item">
          <div className="seat seat-booked"></div>
          <span>Booked</span>
        </div>
      </div>
    </div>
  );
};
```

**File:** `frontend/src/components/SeatMap/SeatMap.css`

```css
.seat-map {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.screen {
  background: linear-gradient(to bottom, #333, #666);
  color: white;
  text-align: center;
  padding: 10px;
  margin-bottom: 40px;
  border-radius: 20px 20px 0 0;
  font-weight: bold;
}

.seats-container {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.seat-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.row-label {
  width: 30px;
  font-weight: bold;
  text-align: center;
}

.seats {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.seat {
  width: 40px;
  height: 40px;
  border: 2px solid #ccc;
  border-radius: 8px;
  background-color: #f0f0f0;
  cursor: pointer;
  font-size: 11px;
  transition: all 0.2s;
}

.seat:hover:not(:disabled) {
  transform: scale(1.1);
}

.seat-available {
  background-color: #4caf50;
  color: white;
  border-color: #45a049;
}

.seat-selected {
  background-color: #2196f3;
  color: white;
  border-color: #0b7dda;
  transform: scale(1.1);
}

.seat-reserved {
  background-color: #ff9800;
  color: white;
  cursor: not-allowed;
}

.seat-booked {
  background-color: #f44336;
  color: white;
  cursor: not-allowed;
}

.seat:disabled {
  opacity: 0.6;
}

.legend {
  display: flex;
  justify-content: center;
  gap: 20px;
  margin-top: 30px;
  flex-wrap: wrap;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.legend-item .seat {
  width: 30px;
  height: 30px;
  cursor: default;
}
```

### 5.2 Booking Timer Component

**File:** `frontend/src/components/BookingTimer/BookingTimer.tsx`

```typescript
import React, { useEffect, useState } from 'react';

interface BookingTimerProps {
  expiresAt: Date;
  onExpire: () => void;
}

export const BookingTimer: React.FC<BookingTimerProps> = ({ expiresAt, onExpire }) => {
  const [timeLeft, setTimeLeft] = useState<number>(0);

  useEffect(() => {
    const calculateTimeLeft = () => {
      const now = new Date().getTime();
      const expiry = new Date(expiresAt).getTime();
      const diff = expiry - now;
      return Math.max(0, Math.floor(diff / 1000));
    };

    setTimeLeft(calculateTimeLeft());

    const interval = setInterval(() => {
      const remaining = calculateTimeLeft();
      setTimeLeft(remaining);

      if (remaining === 0) {
        clearInterval(interval);
        onExpire();
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [expiresAt, onExpire]);

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  const getTimerClassName = () => {
    if (timeLeft <= 60) return 'timer-critical';
    if (timeLeft <= 120) return 'timer-warning';
    return 'timer-normal';
  };

  return (
    <div className={`booking-timer ${getTimerClassName()}`}>
      <span className="timer-icon">⏱️</span>
      <span className="timer-text">
        Time remaining: {minutes}:{seconds.toString().padStart(2, '0')}
      </span>
    </div>
  );
};
```

---

## Step 6: Key Pages

### 6.1 Seat Selection Page

**File:** `frontend/src/pages/customer/SeatSelectionPage.tsx`

```typescript
import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { SeatMap } from '../../components/SeatMap/SeatMap';
import { BookingTimer } from '../../components/BookingTimer/BookingTimer';
import { bookingApi } from '../../api/bookingApi';
import type { SeatAvailability, Reservation } from '../../types';

export const SeatSelectionPage: React.FC = () => {
  const { showtimeId } = useParams<{ showtimeId: string }>();
  const navigate = useNavigate();

  const [availability, setAvailability] = useState<SeatAvailability | null>(null);
  const [selectedSeats, setSelectedSeats] = useState<string[]>([]);
  const [reservation, setReservation] = useState<Reservation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadSeatAvailability();
  }, [showtimeId]);

  const loadSeatAvailability = async () => {
    try {
      setLoading(true);
      const data = await bookingApi.getSeatAvailability(showtimeId!);
      setAvailability(data);
    } catch (err) {
      setError('Failed to load seat availability');
    } finally {
      setLoading(false);
    }
  };

  const handleSeatClick = (seatNumber: string) => {
    setSelectedSeats((prev) => {
      if (prev.includes(seatNumber)) {
        return prev.filter((s) => s !== seatNumber);
      } else {
        if (prev.length >= 10) {
          alert('Maximum 10 seats per booking');
          return prev;
        }
        return [...prev, seatNumber];
      }
    });
  };

  const handleReserve = async () => {
    if (selectedSeats.length === 0) {
      alert('Please select at least one seat');
      return;
    }

    try {
      const res = await bookingApi.createReservation(showtimeId!, selectedSeats);
      setReservation(res);
    } catch (err: any) {
      alert(err.response?.data?.error || 'Failed to reserve seats');
      // Reload availability in case seats were taken
      await loadSeatAvailability();
      setSelectedSeats([]);
    }
  };

  const handleReservationExpire = async () => {
    alert('Your reservation has expired. Please select seats again.');
    setReservation(null);
    setSelectedSeats([]);
    await loadSeatAvailability();
  };

  const handleProceedToCheckout = () => {
    navigate(`/checkout/${reservation!.id}`);
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!availability) return <div>No data available</div>;

  const allSeats = [
    ...availability.availableSeats,
    ...availability.reservedSeats,
    ...availability.bookedSeats,
  ];

  const totalPrice = selectedSeats.reduce((sum, seatNumber) => {
    const seat = allSeats.find((s) => s.seatNumber === seatNumber);
    return sum + (seat?.price || 0);
  }, 0);

  return (
    <div className="seat-selection-page">
      <h1>Select Your Seats</h1>

      {reservation && (
        <div className="reservation-banner">
          <BookingTimer expiresAt={new Date(reservation.expiresAt)} onExpire={handleReservationExpire} />
          <button onClick={handleProceedToCheckout} className="btn-primary">
            Proceed to Checkout
          </button>
        </div>
      )}

      <SeatMap seats={allSeats} selectedSeats={selectedSeats} onSeatClick={handleSeatClick} />

      <div className="booking-summary">
        <h3>Booking Summary</h3>
        <p>Selected Seats: {selectedSeats.join(', ') || 'None'}</p>
        <p>Total: ${totalPrice.toFixed(2)}</p>

        {!reservation && (
          <button
            onClick={handleReserve}
            disabled={selectedSeats.length === 0}
            className="btn-primary"
          >
            Reserve Seats
          </button>
        )}
      </div>
    </div>
  );
};
```

---

## Step 7: Routing

**File:** `frontend/src/App.tsx`

```typescript
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { Navbar } from './components/Layout/Navbar';

// Pages
import { HomePage } from './pages/customer/HomePage';
import { MoviesPage } from './pages/customer/MoviesPage';
import { SeatSelectionPage } from './pages/customer/SeatSelectionPage';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
};

const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isAdmin } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" />;
  if (!isAdmin) return <Navigate to="/" />;
  return <>{children}</>;
};

const AppRoutes = () => {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/movies" element={<MoviesPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route
          path="/showtime/:showtimeId/seats"
          element={
            <ProtectedRoute>
              <SeatSelectionPage />
            </ProtectedRoute>
          }
        />

        {/* Add more routes */}
      </Routes>
    </BrowserRouter>
  );
};

function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}

export default App;
```

---

## Step 8: Environment Variables

**File:** `frontend/.env`

```
VITE_API_URL=http://localhost:5000/api
```

**File:** `frontend/.env.production`

```
VITE_API_URL=https://your-production-api.com/api
```

---

## Verification Checklist

- [ ] React + Vite project setup
- [ ] API client configured with interceptors
- [ ] Authentication context working
- [ ] Seat map component interactive
- [ ] Booking timer counts down correctly
- [ ] Seat selection page functional
- [ ] Checkout flow completes
- [ ] Admin pages accessible to admins only
- [ ] Responsive design on mobile
- [ ] Environment variables configured

---

## Next Steps

✅ **Phase 5 Complete!**

Proceed to Phase 6: Security & Performance Hardening

See: `docs/phases/phase-6-security-performance.md`
