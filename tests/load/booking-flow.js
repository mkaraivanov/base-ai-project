import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 50 },
    { duration: '3m', target: 50 },
    { duration: '1m', target: 100 },
    { duration: '3m', target: 100 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000/api';

export default function () {
  // Health check
  const healthRes = http.get(`${BASE_URL.replace('/api', '')}/health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
  });

  // Movie listing (cached)
  const moviesRes = http.get(`${BASE_URL}/movies`);
  check(moviesRes, {
    'movies status is 200': (r) => r.status === 200,
    'movies response time < 500ms': (r) => r.timings.duration < 500,
  });

  // Seat availability query (replace with a real showtime ID for actual testing)
  const showtimeId = __ENV.SHOWTIME_ID || '00000000-0000-0000-0000-000000000000';
  const availRes = http.get(`${BASE_URL}/bookings/availability/${showtimeId}`);
  check(availRes, {
    'availability response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
