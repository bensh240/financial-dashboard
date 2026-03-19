export interface WatchlistItem {
  id: number;
  symbol: string;
  companyName: string | null;
  price: number | null;
  change: number | null;
  percentChange: number | null;
  previousClose: number | null;
  addedAt: string;
  lastUpdatedAt: string | null;
}

export interface PriceAlert {
  id: number;
  symbol: string;
  thresholdPercent: number;
  isActive: boolean;
  createdAt: string;
  lastTriggeredAt: string | null;
}

export interface AlertHistory {
  id: number;
  symbol: string;
  priceAtTrigger: number;
  percentChange: number;
  alertType: 'SPIKE' | 'DROP' | 'VOLATILITY' | 'THRESHOLD';
  notes: string | null;
  triggeredAt: string;
  alertId: number | null;
}

export interface Settings {
  email: string;
  dailyBriefTime: string;
  dailyBriefEnabled: boolean;
  weeklyReportEnabled: boolean;
  volatilityThreshold: number;
  sendGridApiKey: string;
  claudeApiKey: string;
}

export interface CandlePoint {
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface StockQuote {
  currentPrice: number;
  change: number;
  percentChange: number;
  high: number;
  low: number;
  open: number;
  previousClose: number;
}

export interface PortfolioItem {
  id: number;
  symbol: string;
  quantity: number;
  avgCostPrice: number;
  addedAt: string;
  notes: string | null;
  currentPrice: number;
  currentValue: number;
  plDollar: number;
  plPercent: number;
}

export interface PortfolioSummary {
  totalInvested: number;
  totalCurrentValue: number;
  totalPlDollar: number;
  totalPlPercent: number;
}
