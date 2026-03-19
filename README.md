# Financial Monitoring Dashboard

A full-stack real-time financial monitoring application with automated alerts, scheduled email reports, and volatility detection.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  React + Vite + TypeScript (port 5173)                  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │Dashboard │ │ Alerts   │ │ History  │ │Settings  │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
└───────────────────────┬─────────────────────────────────┘
                        │ HTTP / REST (proxied by Vite)
┌───────────────────────▼─────────────────────────────────┐
│  .NET 8 Web API (port 5000)                             │
│                                                         │
│  Controllers          Services                          │
│  ├─ WatchlistCtrl     ├─ FinnhubService (HttpClient)   │
│  ├─ AlertsCtrl        ├─ SendGridEmailService           │
│  ├─ StocksCtrl        ├─ PriceMonitorService (hosted)  │
│  └─ SettingsCtrl      ├─ VolatilityDetectorService      │
│                       ├─ DailyBriefService (Hangfire)  │
│  Hangfire Dashboard   └─ WeeklyReportService (Hangfire)│
│  /hangfire                                              │
│                                                         │
│  EF Core + SQLite          Finnhub REST API            │
│  financial_dashboard.db    finnhub.io/api/v1           │
└─────────────────────────────────────────────────────────┘
```

### Tech Decisions

| Concern | Choice | Why |
|---|---|---|
| Backend framework | .NET 8 Web API | Minimal API + controllers, excellent background service support |
| Database | SQLite + EF Core | Zero-ops, file-based, EF migrations for schema management |
| Job scheduling | Hangfire + Hangfire.Storage.SQLite | Persistent jobs survive restarts; built-in dashboard at `/hangfire` |
| Market data | Finnhub REST API | Generous free tier (60 req/min), real-time quotes + candles |
| Email delivery | SendGrid | Reliable transactional email, free tier covers typical alert volume |
| Frontend | React 18 + Vite + TypeScript | Fast HMR, strict typing, minimal config |
| Charts | Recharts | Composable, works natively with React, no canvas quirks |
| Navigation | React Router v6 | Industry standard, nested routing ready |
| Styling | Vanilla CSS custom properties | Zero build overhead, dark theme via CSS vars |

---

## Project Structure

```
/
├── backend/
│   └── FinancialDashboard.API/
│       ├── Controllers/          # REST endpoints
│       ├── Data/                 # EF Core DbContext
│       ├── DTOs/                 # Request/response shapes
│       ├── Migrations/           # EF Core SQL migrations
│       ├── Models/               # Domain entities
│       ├── Services/             # Business logic + background workers
│       ├── appsettings.json
│       ├── .env.example
│       └── Program.cs            # DI wiring + middleware pipeline
│
└── frontend/
    └── dashboard-client/
        ├── src/
        │   ├── components/       # Reusable UI (table, chart, modal)
        │   ├── pages/            # Route-level views
        │   ├── services/api.ts   # Typed fetch wrapper
        │   └── types/index.ts    # Shared TypeScript types
        ├── .env.example
        └── vite.config.ts        # Dev proxy → backend
```

---

## Background Automation Services

### 1. PriceMonitorService (`IHostedService`)

- **Frequency:** Every 5 minutes (configurable constant)
- **Mechanism:** Registered as `BackgroundService`; survives app restarts without Hangfire
- **What it does:**
  1. Loads all watchlist symbols from SQLite
  2. Fetches quotes from Finnhub (200 ms delay between calls to respect free-tier rate limits)
  3. Updates cached price columns on `WatchlistItems`
  4. Evaluates every active `PriceAlert` — triggers if `|percentChange| >= threshold`
  5. Delegates each price delta to `VolatilityDetectorService`
  6. Persists new `AlertHistory` rows and emails a digest if any fired

### 2. VolatilityDetectorService (singleton + Hangfire)

- **Mechanism:** Singleton service injected into `PriceMonitorService`; its `Detect()` method is called inside every poll cycle
- **What it does:** Compares each stock's current price to its last cached price. If the intra-cycle swing exceeds `UserSettings.VolatilityThreshold` (default 3%), a `VOLATILITY` alert is logged
- **Cooldown:** 30-minute per-symbol cooldown (in-memory dictionary) prevents alert spam
- **Hangfire summary job:** Every 15 minutes, logs which symbols were recently flagged

### 3. DailyBriefService (Hangfire recurring job)

- **Cron:** `0 {hour} * * *` — defaults to 8 AM UTC, reconfigured live via the Settings page
- **What it does:**
  1. Fetches fresh quotes for the whole watchlist
  2. Builds a structured HTML email: top gainers, top losers, full watchlist table, last 24 h alerts
  3. Sends via `IEmailService` (SendGrid)
- **AI Enhancement point:** The `GenerateRuleSummary()` method produces a rule-based narrative. Replace it with a Claude API call (`POST /v1/messages`) to get GPT-style prose from the stock data

### 4. WeeklyReportService (Hangfire recurring job)

- **Cron:** `0 9 * * 0` — every Sunday at 9 AM UTC
- **What it does:** Aggregates the past 7 days of `AlertHistory`, computes per-symbol stats (alert count, max swing), and emails a performance summary

---

## API Endpoints

### Watchlist
| Method | Path | Description |
|---|---|---|
| GET | `/api/watchlist` | List all items with live Finnhub prices |
| POST | `/api/watchlist` | Add stock (`{ symbol }`) — validates via Finnhub |
| DELETE | `/api/watchlist/{symbol}` | Remove stock |

### Alerts
| Method | Path | Description |
|---|---|---|
| GET | `/api/alerts` | List all configured alerts |
| POST | `/api/alerts` | Create alert (`{ symbol, thresholdPercent }`) |
| PUT | `/api/alerts/{id}` | Toggle active / update threshold |
| DELETE | `/api/alerts/{id}` | Delete alert |
| GET | `/api/alerts/history` | History log (`?symbol=&limit=`) |

### Stocks
| Method | Path | Description |
|---|---|---|
| GET | `/api/stocks/{symbol}/quote` | Live Finnhub quote |
| GET | `/api/stocks/{symbol}/profile` | Company profile |
| GET | `/api/stocks/{symbol}/candles` | OHLCV history (`?resolution=D&days=30`) |

### Settings
| Method | Path | Description |
|---|---|---|
| GET | `/api/settings` | Get current settings |
| PUT | `/api/settings` | Save + reschedule Hangfire jobs |

**Swagger UI:** `http://localhost:5000/swagger`
**Hangfire Dashboard:** `http://localhost:5000/hangfire`

---

## Setup & Running

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- A [Finnhub](https://finnhub.io) API key (free)
- A [SendGrid](https://sendgrid.com) API key (free tier, optional – required for emails)

### Backend

```bash
cd backend/FinancialDashboard.API

# Copy and fill in secrets
cp .env.example .env
# Edit .env: set Finnhub__ApiKey and SendGrid__ApiKey

# Restore packages
dotnet restore

# Run (auto-migrates DB on startup)
dotnet run
```

The API starts on `http://localhost:5000`.

### Frontend

```bash
cd frontend/dashboard-client

# Copy env
cp .env.example .env

# Install dependencies
npm install

# Start dev server
npm run dev
```

The app opens at `http://localhost:5173`. Vite proxies `/api/*` to the backend automatically.

### Production Build

```bash
# Frontend
npm run build   # outputs to dist/

# Backend
dotnet publish -c Release -o ./publish
```

---

## Environment Variables

### Backend (`backend/FinancialDashboard.API/.env`)

| Variable | Required | Description |
|---|---|---|
| `Finnhub__ApiKey` | Yes | Finnhub API key |
| `SendGrid__ApiKey` | For email | SendGrid API key |
| `SendGrid__FromEmail` | For email | Verified sender email |
| `SendGrid__FromName` | No | Display name |
| `AllowedOrigins` | No | CORS origin (default: `http://localhost:5173`) |
| `Database__Path` | No | SQLite file path (default: `financial_dashboard.db`) |

### Frontend (`frontend/dashboard-client/.env`)

| Variable | Required | Description |
|---|---|---|
| `VITE_API_BASE_URL` | No | Backend URL when not using Vite proxy |
| `VITE_POLL_INTERVAL_MS` | No | Watchlist auto-refresh interval (default: 60000) |

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
LastPercentChange                       TriggeredAt
LastPriceUpdatedAt                      AlertId
```

---

## Extending the Project

### Add Claude AI summaries

In `DailyBriefService.cs`, replace `GenerateRuleSummary()` with:

```csharp
// POST https://api.anthropic.com/v1/messages
var prompt = $"Write a concise 2-sentence market brief for a retail investor based on this data: {JsonSerializer.Serialize(quotes)}";
// Use HttpClient with header: x-api-key: {AnthropicApiKey}
```

### Add more alert types

Extend `AlertType` values and add detection logic in `PriceMonitorService.PollAsync()`.

### Add user authentication

Add ASP.NET Core Identity or JWT middleware to `Program.cs` and protect controllers with `[Authorize]`.
