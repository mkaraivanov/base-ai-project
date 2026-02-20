import axios from 'axios';
import i18n from '../i18n';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5076/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token and Accept-Language header
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    const lang = i18n.language?.split('-')[0] ?? 'en';
    config.headers['Accept-Language'] = lang;
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor to handle errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Don't redirect on auth endpoints — let the caller handle the error
      const requestUrl = error.config?.url ?? '';
      const isAuthEndpoint = requestUrl.startsWith('/auth/');
      if (!isAuthEndpoint) {
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  },
);

export default apiClient;
