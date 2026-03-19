import type {
  AlertHistory,
  CandlePoint,
  PriceAlert,
  Settings,
  WatchlistItem,
} from '../types';

const BASE = import.meta.env.VITE_API_BASE_URL ?? '';

async function request<T>(
  path: string,
  options?: RequestInit,
): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err?.error ?? `HTTP ${res.status}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ── Watchlist ─────────────────────────────────────────────────────────────────
export const api = {
  watchlist: {
    list: ()               => request<WatchlistItem[]>('/api/watchlist'),
    add:  (symbol: string) => request<WatchlistItem>('/api/watchlist', {
      method: 'POST',
      body: JSON.stringify({ symbol }),
    }),
    remove: (symbol: string) => request<void>(`/api/watchlist/${symbol}`, { method: 'DELETE' }),
  },

  // ── Alerts ──────────────────────────────────────────────────────────────────
  alerts: {
    list: () => request<PriceAlert[]>('/api/alerts'),
    create: (symbol: string, thresholdPercent: number) =>
      request<PriceAlert>('/api/alerts', {
        method: 'POST',
        body: JSON.stringify({ symbol, thresholdPercent }),
      }),
    update: (id: number, thresholdPercent: number, isActive: boolean) =>
      request<PriceAlert>(`/api/alerts/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ thresholdPercent, isActive }),
      }),
    delete: (id: number) => request<void>(`/api/alerts/${id}`, { method: 'DELETE' }),
    history: (symbol?: string, limit = 100) =>
      request<AlertHistory[]>(
        `/api/alerts/history?limit=${limit}${symbol ? `&symbol=${symbol}` : ''}`,
      ),
  },

  // ── Stocks ──────────────────────────────────────────────────────────────────
  stocks: {
    candles: (symbol: string, resolution = 'D', days = 30) =>
      request<CandlePoint[]>(`/api/stocks/${symbol}/candles?resolution=${resolution}&days=${days}`),
    search: (q: string) =>
      request<{ symbol: string; description: string }[]>(`/api/stocks/search?q=${encodeURIComponent(q)}`),
  },

  // ── Settings ─────────────────────────────────────────────────────────────────
  settings: {
    get:    ()               => request<Settings>('/api/settings'),
    update: (s: Settings)   => request<Settings>('/api/settings', {
      method: 'PUT',
      body: JSON.stringify(s),
    }),
  },
};
