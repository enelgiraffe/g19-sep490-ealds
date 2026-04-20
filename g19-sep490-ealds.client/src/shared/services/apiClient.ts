import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';

import type { LoginResponse } from '../../modules/auth/types/auth.types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

/** Marks a request that already retried after refresh (axios config extension). */
type RetryableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

let refreshPromise: Promise<string | null> | null = null;

export function clearSessionAndRedirectToLogin(): void {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  const base = import.meta.env.BASE_URL ?? '/';
  const normalized = base.endsWith('/') ? base.slice(0, -1) : base;
  window.location.href = `${normalized}/login`;
}

async function refreshAccessToken(): Promise<string | null> {
  if (refreshPromise) return refreshPromise;
  refreshPromise = (async () => {
    try {
      const rt = localStorage.getItem('refreshToken');
      if (!rt) return null;
      const { data } = await axios.post<LoginResponse>(
        `${API_BASE_URL}/api/auth/refresh`,
        { refreshToken: rt },
        { headers: { 'Content-Type': 'application/json' } },
      );
      localStorage.setItem('accessToken', data.accessToken);
      if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken);
      if (data.user) localStorage.setItem('user', JSON.stringify(data.user));
      return data.accessToken;
    } catch {
      return null;
    } finally {
      refreshPromise = null;
    }
  })();
  return refreshPromise;
}

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

function shouldSkip401Refresh(url: string): boolean {
  const u = url.toLowerCase();
  return (
    u.includes('/api/auth/login') ||
    u.includes('/api/auth/refresh') ||
    u.includes('/api/auth/forgot-password') ||
    u.includes('/api/auth/verify-otp') ||
    u.includes('/api/auth/reset-password')
  );
}

apiClient.interceptors.response.use(
  (response) => {
    const url = response.config.url ?? '';
    if (
      url.includes('/complete') ||
      url.includes('/confirm') ||
      url.includes('/director-approve') ||
      url.includes('/reject') ||
      url.includes('/cancel') ||
      url.includes('/apply-actual')
    ) {
      window.dispatchEvent(new Event('ealds-notifications-changed'));
    }
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableConfig | undefined;
    if (!originalRequest) return Promise.reject(error);

    const path = originalRequest.url ?? '';
    const absolute = `${originalRequest.baseURL ?? ''}${path}`;

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }
    if (shouldSkip401Refresh(path) || shouldSkip401Refresh(absolute)) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;
    const newToken = await refreshAccessToken();
    if (!newToken) {
      clearSessionAndRedirectToLogin();
      return Promise.reject(error);
    }
    originalRequest.headers = originalRequest.headers ?? {};
    originalRequest.headers.Authorization = `Bearer ${newToken}`;
    return apiClient(originalRequest);
  },
);

/** JWT `exp` (seconds), or null if missing / invalid */
function getJwtExpSeconds(token: string): number | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
    const payload = JSON.parse(json) as { exp?: number };
    return typeof payload.exp === 'number' ? payload.exp : null;
  } catch {
    return null;
  }
}

/** Refresh access token this many seconds before it expires (when user is active). */
const REFRESH_BEFORE_EXPIRY_SEC = 120;
/** If the user has been idle longer than this, log out (no API activity / interaction). */
const IDLE_LOGOUT_MS = 30 * 60 * 1000;
/** Only run proactive refresh if the user interacted within this window. */
const PROACTIVE_REFRESH_ACTIVITY_MAX_MS = 5 * 60 * 1000;

/**
 * Call once at app root: logs out after idle timeout; proactively refreshes the access token
 * before expiry while the user is active so sessions keep working during continuous use.
 */
export function initSessionMonitoring(): () => void {
  let lastActivity = Date.now();

  const recordActivity = () => {
    lastActivity = Date.now();
  };

  const events: (keyof WindowEventMap)[] = ['mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'];
  for (const e of events) {
    window.addEventListener(e, recordActivity, { passive: true });
  }

  const interval = window.setInterval(() => {
    if (!localStorage.getItem('accessToken')) return;

    if (Date.now() - lastActivity > IDLE_LOGOUT_MS) {
      clearSessionAndRedirectToLogin();
      return;
    }

    const token = localStorage.getItem('accessToken');
    if (!token || !localStorage.getItem('refreshToken')) return;

    const exp = getJwtExpSeconds(token);
    if (!exp) return;

    const nowSec = Date.now() / 1000;
    const secondsLeft = exp - nowSec;
    const recentlyActive = Date.now() - lastActivity < PROACTIVE_REFRESH_ACTIVITY_MAX_MS;

    if (recentlyActive && secondsLeft > 0 && secondsLeft < REFRESH_BEFORE_EXPIRY_SEC) {
      void refreshAccessToken();
    }
  }, 60_000);

  return () => {
    window.clearInterval(interval);
    for (const e of events) {
      window.removeEventListener(e, recordActivity);
    }
  };
}
