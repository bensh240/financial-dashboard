import { useEffect, useRef, useState } from 'react'
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

type ChartMode = 'line' | 'candles'

interface CandleTooltip {
  x: number
  y: number
  candle: CandlePoint
}

function CandlestickChart({ data }: { data: CandlePoint[] }) {
  const svgRef = useRef<SVGSVGElement>(null)
  const [tooltip, setTooltip] = useState<CandleTooltip | null>(null)

  if (data.length === 0) return null

  const width      = 900
  const height     = 260
  const paddingLeft   = 70
  const paddingRight  = 16
  const paddingTop    = 10
  const paddingBottom = 30

  const chartWidth  = width - paddingLeft - paddingRight
  const chartHeight = height - paddingTop - paddingBottom

  const highs  = data.map(d => d.high)
  const lows   = data.map(d => d.low)
  const minVal = Math.min(...lows)
  const maxVal = Math.max(...highs)
  const range  = maxVal - minVal || 1

  const toY = (v: number) =>
    paddingTop + chartHeight - ((v - minVal) / range) * chartHeight

  const candleWidth = Math.max(2, Math.min(12, (chartWidth / data.length) * 0.7))
  const gap = chartWidth / data.length

  // Y-axis labels
  const yTicks = 4
  const yLabels = Array.from({ length: yTicks }, (_, i) => {
    const val = minVal + (range * i) / (yTicks - 1)
    return { val, y: toY(val) }
  })

  // X-axis labels (~5 evenly spaced)
  const xIndices = Array.from({ length: Math.min(5, data.length) }, (_, i) =>
    Math.round((i / (Math.min(5, data.length) - 1 || 1)) * (data.length - 1))
  )

  const formatDate = (ts: string) =>
    new Date(ts).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })

  return (
    <div style={{ position: 'relative' }}>
      <svg
        ref={svgRef}
        viewBox={`0 0 ${width} ${height}`}
        style={{ width: '100%', height: 260, display: 'block' }}
        onMouseLeave={() => setTooltip(null)}
      >
        {/* Grid lines */}
        {yLabels.map((label, i) => (
          <line
            key={i}
            x1={paddingLeft}
            x2={width - paddingRight}
            y1={label.y}
            y2={label.y}
            stroke="rgba(255,255,255,0.05)"
            strokeDasharray="3 3"
          />
        ))}

        {/* Y-axis labels */}
        {yLabels.map((label, i) => (
          <text
            key={i}
            x={paddingLeft - 6}
            y={label.y + 4}
            textAnchor="end"
            fill="#8892a4"
            fontSize={11}
          >
            ${label.val.toFixed(0)}
          </text>
        ))}

        {/* X-axis labels */}
        {xIndices.map(idx => {
          const cx = paddingLeft + idx * gap + gap / 2
          return (
            <text
              key={idx}
              x={cx}
              y={height - 6}
              textAnchor="middle"
              fill="#8892a4"
              fontSize={11}
            >
              {formatDate(data[idx].timestamp)}
            </text>
          )
        })}

        {/* Candles */}
        {data.map((candle, i) => {
          const cx     = paddingLeft + i * gap + gap / 2
          const isGreen = candle.close >= candle.open
          const color   = isGreen ? '#22c55e' : '#ef4444'
          const bodyTop = toY(Math.max(candle.open, candle.close))
          const bodyBot = toY(Math.min(candle.open, candle.close))
          const bodyH   = Math.max(1, bodyBot - bodyTop)

          return (
            <g
              key={i}
              onMouseEnter={_e => {
                if (!svgRef.current) return
                const rect = svgRef.current.getBoundingClientRect()
                const scaleX = rect.width / width
                const scaleY = rect.height / height
                setTooltip({
                  x: cx * scaleX,
                  y: toY(candle.high) * scaleY,
                  candle
                })
              }}
            >
              {/* Wick */}
              <line
                x1={cx}
                x2={cx}
                y1={toY(candle.high)}
                y2={toY(candle.low)}
                stroke={color}
                strokeWidth={1}
              />
              {/* Body */}
              <rect
                x={cx - candleWidth / 2}
                y={bodyTop}
                width={candleWidth}
                height={bodyH}
                fill={color}
                rx={1}
              />
            </g>
          )
        })}
      </svg>

      {/* Hover tooltip */}
      {tooltip && (
        <div style={{
          position:     'absolute',
          left:         tooltip.x + 12,
          top:          Math.max(0, tooltip.y - 10),
          background:   '#1a1d27',
          border:       '1px solid #2a2d3a',
          borderRadius: 8,
          padding:      '8px 12px',
          fontSize:     12,
          pointerEvents: 'none',
          zIndex:       10,
          whiteSpace:   'nowrap',
          color:        '#e2e8f0',
        }}>
          <div style={{ fontWeight: 700, marginBottom: 4 }}>
            {formatDate(tooltip.candle.timestamp)}
          </div>
          <div>O: <strong>${tooltip.candle.open.toFixed(2)}</strong></div>
          <div>H: <strong style={{ color: '#22c55e' }}>${tooltip.candle.high.toFixed(2)}</strong></div>
          <div>L: <strong style={{ color: '#ef4444' }}>${tooltip.candle.low.toFixed(2)}</strong></div>
          <div>C: <strong>${tooltip.candle.close.toFixed(2)}</strong></div>
        </div>
      )}
    </div>
  )
}

export default function StockChart({ symbol }: Props) {
  const [data,       setData]       = useState<CandlePoint[]>([])
  const [resolution, setResolution] = useState(RESOLUTIONS[1])
  const [chartMode,  setChartMode]  = useState<ChartMode>('line')
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
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16, flexWrap: 'wrap', gap: 10 }}>
        <h3 style={{ fontWeight: 700 }}>{symbol} – Price History</h3>

        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
          {/* Chart mode toggle */}
          <div style={{ display: 'flex', gap: 4, marginRight: 8 }}>
            <button
              className={`btn btn-sm ${chartMode === 'line' ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setChartMode('line')}
            >
              Line
            </button>
            <button
              className={`btn btn-sm ${chartMode === 'candles' ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setChartMode('candles')}
            >
              Candles
            </button>
          </div>

          {/* Time range */}
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
        chartMode === 'line' ? (
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
        ) : (
          <CandlestickChart data={data} />
        )
      )}

      {!loading && !error && data.length === 0 && (
        <div className="empty-state" style={{ padding: 40 }}>
          <p>No historical data available. Finnhub free tier requires market hours.</p>
        </div>
      )}
    </div>
  )
}
