import { useEffect, useState } from 'react'
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts'
import { api } from '../services/api'
import type { CandlePoint } from '../types'

interface Props { symbol: string }

const RESOLUTIONS = [
  { label: '1W', resolution: 'D', days: 7 },
  { label: '1M', resolution: 'D', days: 30 },
  { label: '3M', resolution: 'D', days: 90 },
  { label: '1Y', resolution: 'W', days: 365 },
]

export default function StockChart({ symbol }: Props) {
  const [data,       setData]       = useState<CandlePoint[]>([])
  const [resolution, setResolution] = useState(RESOLUTIONS[1])
  const [loading,    setLoading]    = useState(false)
  const [error,      setError]      = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    api.stocks.candles(symbol, resolution.resolution, resolution.days)
      .then(d => { if (!cancelled) setData(d) })
      .catch(e => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })

    return () => { cancelled = true }
  }, [symbol, resolution])

  const isUp = data.length >= 2 && data[data.length - 1].close >= data[0].close
  const color = isUp ? '#22c55e' : '#ef4444'

  const formatDate = (ts: string) =>
    new Date(ts).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })

  return (
    <div className="card chart-container">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 }}>
        <h3 style={{ fontWeight: 700 }}>{symbol} – Price History</h3>
        <div style={{ display: 'flex', gap: 6 }}>
          {RESOLUTIONS.map(r => (
            <button
              key={r.label}
              className={`btn btn-sm ${resolution.label === r.label ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setResolution(r)}
            >
              {r.label}
            </button>
          ))}
        </div>
      </div>

      {loading && <div className="loading-screen"><div className="spinner" /> Loading chart…</div>}
      {error   && <div className="error-banner">{error}</div>}

      {!loading && !error && data.length > 0 && (
        <ResponsiveContainer width="100%" height={260}>
          <AreaChart data={data} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
            <defs>
              <linearGradient id="chartGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%"  stopColor={color} stopOpacity={0.25} />
                <stop offset="95%" stopColor={color} stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
            <XAxis
              dataKey="timestamp"
              tickFormatter={formatDate}
              tick={{ fill: '#8892a4', fontSize: 11 }}
              axisLine={false}
              tickLine={false}
              interval="preserveStartEnd"
            />
            <YAxis
              domain={['auto', 'auto']}
              tick={{ fill: '#8892a4', fontSize: 11 }}
              axisLine={false}
              tickLine={false}
              tickFormatter={v => `$${v.toFixed(0)}`}
              width={60}
            />
            <Tooltip
              contentStyle={{ background: '#1a1d27', border: '1px solid #2a2d3a', borderRadius: 8, fontSize: 13 }}
              labelFormatter={formatDate}
              formatter={(v: number) => [`$${v.toFixed(2)}`, 'Close']}
            />
            <Area
              type="monotone"
              dataKey="close"
              stroke={color}
              strokeWidth={2}
              fill="url(#chartGrad)"
              dot={false}
              activeDot={{ r: 4, strokeWidth: 0 }}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}

      {!loading && !error && data.length === 0 && (
        <div className="empty-state" style={{ padding: 40 }}>
          <p>No historical data available. Finnhub free tier requires market hours.</p>
        </div>
      )}
    </div>
  )
}
