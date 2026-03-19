import { useEffect, useState } from 'react'
import { api } from '../services/api'
import type { AlertHistory } from '../types'

type AlertTypeFilter = 'ALL' | 'SPIKE' | 'DROP' | 'VOLATILITY'

function TypeBadge({ type }: { type: string }) {
  const map: Record<string, string> = {
    SPIKE:      'badge-green',
    DROP:       'badge-red',
    VOLATILITY: 'badge-yellow',
    THRESHOLD:  'badge-blue',
  }
  return <span className={`badge ${map[type] ?? 'badge-gray'}`}>{type}</span>
}

export default function AlertHistory() {
  const [history,   setHistory]   = useState<AlertHistory[]>([])
  const [loading,   setLoading]   = useState(true)
  const [error,     setError]     = useState<string | null>(null)
  const [typeFilter, setTypeFilter] = useState<AlertTypeFilter>('ALL')
  const [symFilter,  setSymFilter]  = useState('')

  useEffect(() => {
    api.alerts.history(undefined, 500)
      .then(setHistory)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  const filtered = history.filter(h => {
    const matchType = typeFilter === 'ALL' || h.alertType === typeFilter
    const matchSym  = !symFilter || h.symbol.includes(symFilter.toUpperCase())
    return matchType && matchSym
  })

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">Alert History</h1>
        <span style={{ color: 'var(--muted)', fontSize: 13 }}>{filtered.length} entries</span>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Filters */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 20 }}>
        <input
          className="form-input"
          style={{ maxWidth: 180 }}
          placeholder="Filter by symbol…"
          value={symFilter}
          onChange={e => setSymFilter(e.target.value)}
        />
        {(['ALL', 'SPIKE', 'DROP', 'VOLATILITY'] as AlertTypeFilter[]).map(t => (
          <button
            key={t}
            className={`btn btn-sm ${typeFilter === t ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setTypeFilter(t)}
          >
            {t}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="loading-screen"><div className="spinner" /> Loading history…</div>
      ) : filtered.length === 0 ? (
        <div className="empty-state">
          <h3>No alert history</h3>
          <p>Alert events will appear here once price monitors trigger.</p>
        </div>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Symbol</th>
                <th>Type</th>
                <th>% Change</th>
                <th>Price at Trigger</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(h => (
                <tr key={h.id}>
                  <td style={{ color: 'var(--muted)', fontSize: 12, whiteSpace: 'nowrap' }}>
                    {new Date(h.triggeredAt).toLocaleString()}
                  </td>
                  <td><span style={{ fontWeight: 700, color: 'var(--accent)' }}>{h.symbol}</span></td>
                  <td><TypeBadge type={h.alertType} /></td>
                  <td>
                    <span className={h.percentChange >= 0 ? 'positive' : 'negative'}>
                      {h.percentChange >= 0 ? '+' : ''}{h.percentChange.toFixed(2)}%
                    </span>
                  </td>
                  <td>${h.priceAtTrigger.toFixed(2)}</td>
                  <td style={{ color: 'var(--muted)', fontSize: 12, maxWidth: 260, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {h.notes ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
