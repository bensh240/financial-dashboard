# Financial Monitoring Dashboard

A full-stack personal financial monitoring dashboard with real-time prices, automated email reports, AI-powered summaries, portfolio tracking, and market news.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  React + Vite + TypeScript (port 5173)                          │
│  ┌──────────┐ ┌───────────┐ ┌──────┐ ┌─────────┐ ┌─────────┐  │
│  │Dashboard │ │ Portfolio │ │ News │ │ Alerts  │ │Settings │  │
│  └──────────┘ └───────────┘ └──────┘ └─────────┘ └─────────┘  │
└───────────────────────┬─────────────────────────────────────────┘
                        │ HTTP / REST (proxied by Vite)
┌───────────────────────▼─────────────────────────────────────────┐
│  .NET 10 Web API (port 5100)                                    │
│                                                                 │
│  Controllers              Services                              │
│  ├─ AuthCtrl              ├─ FinnhubService (quotes, news)      │
│  ├─ WatchlistCtrl         ├─ SendGridEmailService               │
│  ├─ AlertsCtrl            ├─ PriceMonitorService (hosted)       │
│  ├─ StocksCtrl            ├─ VolatilityDetectorService          │
│  ├─ PortfolioCtrl         ├─ DailyBriefService (Hangfire + AI)  │
│  ├─ NewsCtrl              └─ WeeklyReportService (Hangfire)     │
│  └─ SettingsCtrl                                                │
│                                                                 │
│  Hangfire Dashboard /hangfire     EF Core + SQLite              │
│  Finnhub API  ·  Yahoo Finance    Claude API (Anthropic)        │
└─────────────────────────────────────────────────────────────────┘
```

### Tech Stack

| Concern | Choice |
|---|---|
| Backend | .NET 10 Web API |
| Auth | JWT Bearer tokens |
| Database | SQLite + EF Core 9 |
| Job scheduling | Hangfire + Hangfire.Storage.SQLite |
| Market data | Finnhub REST API (quotes, search, news) |
| Historical charts | Yahoo Finance (free, no key required) |
| Email delivery | SendGrid |
| AI summaries | Claude API (Anthropic) |
| Frontend | React 18 + Vite + TypeScript |
| Charts | Recharts (line) + custom SVG (candlestick) |
| Styling | Vanilla CSS custom properties (dark theme) |

---

## Features

### Dashboard
- Watchlist with live prices, % change, previous close
- Add stocks by symbol or company name (autocomplete search)
- International stocks supported (e.g. `ASML.AS`, `7203.T`)
- Click any stock to open an interactive price chart
- Browser push notifications for price alerts

### Price Charts
- **Line mode** — Recharts area chart with gradient fill
- **Candlestick mode** — custom SVG with OHLCV, wicks, hover tooltip
- Time ranges: 1W / 1M / 3M / 1Y (via Yahoo Finance, free)

### Portfolio
- Track positions: symbol, quantity, average cost
- Real-time P&L in $ and % per position
- Summary cards: Total Invested, Current Value, Total P&L
- Add, edit, and delete positions

### News
- **Market News** — general, forex, crypto, M&A categories
- **Stock News** — last 7 days of news per watchlist symbol
- Article cards with image, headline, summary, source, and link

### Alerts
- Price threshold alerts per symbol (% change)
- Toggle alerts on/off
- Full alert history log (SPIKE / DROP / VOLATILITY / THRESHOLD)

### Automated Emails
- **Daily Brief** — morning summary with top movers, watchlist table, recent alerts, and optional Claude AI narrative
- **Weekly Report** — Sunday performance summary (alert counts, max swings)
- **Volatility Alerts** — immediate email when a stock swings beyond threshold in one polling cycle

### Settings
- Recipient email address
- Daily brief time (UTC) — reschedules Hangfire job on save
- Volatility threshold (%)
- Enable/disable daily brief and weekly report
- Change login username and password

---

## API Endpoints

All endpoints require JWT auth except `POST /api/auth/login`.

### Auth
| Method | Path | Description |
|---|---|---|
| POST | `/api/auth/login` | Login, returns JWT |
| POST | `/api/auth/logout` | Logout (client discards token) |
| GET | `/api/auth/me` | Current user info |

### Watchlist
| Method | Path | Description |
|---|---|---|
| GET | `/api/watchlist` | List all items with live prices |
| POST | `/api/watchlist` | Add stock `{ symbol }` |
| DELETE | `/api/watchlist/{symbol}` | Remove stock |

### Alerts
| Method | Path | Description |
|---|---|---|
| GET | `/api/alerts` | List alerts |
| POST | `/api/alerts` | Create `{ symbol, thresholdPercent }` |
| PUT | `/api/alerts/{id}` | Update threshold / toggle active |
| DELETE | `/api/alerts/{id}` | Delete |
| GET | `/api/alerts/history` | History log (`?symbol=&limit=`) |

### Stocks
| Method | Path | Description |
|---|---|---|
| GET | `/api/stocks/search` | Symbol search `?q=` |
| GET | `/api/stocks/{symbol}/quote` | Live quote |
| GET | `/api/stocks/{symbol}/candles` | OHLCV history `?resolution=D&days=30` |

### News
| Method | Path | Description |
|---|---|---|
| GET | `/api/news/market` | Market news `?category=general` |
| GET | `/api/news/stock/{symbol}` | Company news (last 7 days) |

### Portfolio
| Method | Path | Description |
|---|---|---|
| GET | `/api/portfolio` | List positions with live P&L |
| GET | `/api/portfolio/summary` | Aggregate totals |
| POST | `/api/portfolio` | Add position |
| PUT | `/api/portfolio/{id}` | Update position |
| DELETE | `/api/portfolio/{id}` | Delete position |

### Settings
| Method | Path | Description |
|---|---|---|
| GET | `/api/settings` | Get settings |
| PUT | `/api/settings` | Save + reschedule jobs |

**Swagger UI:** `http://localhost:5100/swagger`
**Hangfire Dashboard:** `http://localhost:5100/hangfire`

---

## Setup & Running

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Finnhub](https://finnhub.io) API key (free)
- [SendGrid](https://sendgrid.com) API key + verified sender email (free tier)
- [Claude API](https://console.anthropic.com) key (optional, for AI summaries)

### Backend

```bash
cd backend/FinancialDashboard.API

# Edit .env with your keys
nano .env

# Restore & run (DB is created automatically on first start)
dotnet restore
dotnet run
```

The API starts on `http://localhost:5100`.

**Default login:** `admin` / `admin123` — change in Settings after first login.

### Frontend

```bash
cd frontend/dashboard-client
npm install
npm run dev
```

Opens at `http://localhost:5173`. Vite proxies `/api/*` to the backend.

### Production Build

```bash
# Frontend
cd frontend/dashboard-client && npm run build   # → dist/

# Backend
cd backend/FinancialDashboard.API && dotnet publish -c Release -o ./publish
```

---

## Environment Variables

### Backend (`backend/FinancialDashboard.API/.env`)

| Variable | Required | Description |
|---|---|---|
| `Finnhub__ApiKey` | Yes | Finnhub API key |
| `SendGrid__ApiKey` | For email | SendGrid API key |
| `SendGrid__FromEmail` | For email | Verified sender address |
| `SendGrid__FromName` | No | Display name in emails |
| `Anthropic__ApiKey` | No | Claude API key for AI summaries |
| `AllowedOrigins` | No | CORS origin (default: `http://localhost:5173`) |
| `Database__Path` | No | SQLite file path (default: `financial_dashboard.db`) |

---

## Database Schema

```
WatchlistItems      PriceAlerts         AlertHistories      UserSettings
──────────────      ───────────         ──────────────      ────────────
Id                  Id                  Id                  Id
Symbol              Symbol              Symbol              Email
CompanyName         ThresholdPercent    PriceAtTrigger      DailyBriefTime
AddedAt             IsActive            PercentChange       DailyBriefEnabled
LastKnownPrice      CreatedAt           AlertType           WeeklyReportEnabled
PreviousClose       LastTriggeredAt     Notes               VolatilityThreshold
LastPercentChange                       TriggeredAt         AdminUsername
LastPriceUpdatedAt                      AlertId             AdminPassword

PortfolioItems
──────────────
Id
Symbol
Quantity
AvgCostPrice
AddedAt
Notes
```

---

## Background Services

| Service | Schedule | Description |
|---|---|---|
| `PriceMonitorService` | Every 5 min | Polls Finnhub, updates prices, fires threshold/volatility alerts |
| `VolatilityDetectorService` | Per poll cycle + every 15 min | Detects intra-cycle price swings, 30-min cooldown per symbol |
| `DailyBriefService` | Configurable (default 8 AM UTC) | Sends morning email with movers, watchlist, alerts, AI summary |
| `WeeklyReportService` | Sundays 9 AM UTC | Sends weekly performance summary email |
