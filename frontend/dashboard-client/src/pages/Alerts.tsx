import { useEffect, useState, type FormEvent } from 'react'
import { Plus, Trash2, PauseCircle, PlayCircle } from 'lucide-react'
import { api } from '../services/api'
import type { PriceAlert, WatchlistItem } from '../types'

export default function Alerts() {
  const [alerts,    setAlerts]    = useState<PriceAlert[]>([])
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([])
  const [loading,   setLoading]   = useState(true)
  const [error,     setError]     = useState<string | null>(null)
  const [showForm,  setShowForm]  = useState(false)

  // Form state
  const [fSymbol,    setFSymbol]    = useState('')
  const [fThreshold, setFThreshold] = useState('5')
  const [fLoading,   setFLoading]   = useState(false)
  const [fError,     setFError]     = useState<string | null>(null)

  useEffect(() => {
    Promise.all([api.alerts.list(), api.watchlist.list()])
      .then(([a, w]) => { setAlerts(a); setWatchlist(w) })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setFLoading(true)
    setFError(null)
    try {
      const created = await api.alerts.create(fSymbol, Number(fThreshold))
      setAlerts(prev => [...prev, created])
      setShowForm(false)
      setFSymbol('')
      setFThreshold('5')
    } catch (err: unknown) {
      setFError(err instanceof Error ? err.message : 'Failed to create alert')
    } finally {
      setFLoading(false)
    }
  }

  async function handleToggle(alert: PriceAlert) {
    const updated = await api.alerts.update(alert.id, alert.thresholdPercent, !alert.isActive)
    setAlerts(prev => prev.map(a => a.id === alert.id ? updated : a))
  }

  async function handleDelete(id: number) {
    await api.alerts.delete(id)
    setAlerts(prev => prev.filter(a => a.id !== id))
  }

  function AlertTypeBadge({ isActive }: { isActive: boolean }) {
    return isActive
      ? <span className="badge badge-green">Active</span>
      : <span className="badge badge-gray">Paused</span>
  }

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">Price Alerts</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          <Plus size={16} /> New Alert
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Create form */}
      {showForm && (
        <div className="card mb-4" style={{ marginBottom: 20 }}>
          <h3 style={{ fontWeight: 700, marginBottom: 16 }}>Configure Alert</h3>
          {fError && <div className="error-banner">{fError}</div>}
          <form onSubmit={handleCreate}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Stock Symbol</label>
                <select
                  className="form-select"
                  value={fSymbol}
                  onChange={e => setFSymbol(e.target.value)}
                  required
                >
                  <option value="">Choose a stock…</option>
                  {watchlist.map(w => (
                    <option key={w.symbol} value={w.symbol}>{w.symbol} — {w.companyName ?? 'Unknown'}</option>
                  ))}
                </select>
              </div>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Threshold % (e.g. 5 = trigger at ±5%)</label>
                <input
                  type="number"
                  className="form-input"
                  value={fThreshold}
                  onChange={e => setFThreshold(e.target.value)}
                  min="0.1"
                  max="100"
                  step="0.1"
                  required
                />
              </div>
            </div>
            <div style={{ marginTop: 16, display: 'flex', gap: 10 }}>
              <button type="submit" className="btn btn-primary" disabled={fLoading}>
                {fLoading ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Saving…</> : 'Create Alert'}
              </button>
              <button type="button" className="btn btn-ghost" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="loading-screen"><div className="spinner" /> Loading alerts…</div>
      ) : alerts.length === 0 ? (
        <div className="empty-state">
          <h3>No alerts configured</h3>
          <p>Create an alert to get notified when a stock moves by a certain percentage.</p>
        </div>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Threshold</th>
                <th>Status</th>
                <th>Created</th>
                <th>Last Triggered</th>
                <th style={{ width: 100 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {alerts.map(alert => (
                <tr key={alert.id}>
                  <td><span style={{ fontWeight: 700, color: 'var(--accent)' }}>{alert.symbol}</span></td>
                  <td>±{alert.thresholdPercent.toFixed(1)}%</td>
                  <td><AlertTypeBadge isActive={alert.isActive} /></td>
                  <td style={{ color: 'var(--muted)', fontSize: 12 }}>
                    {new Date(alert.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ color: 'var(--muted)', fontSize: 12 }}>
                    {alert.lastTriggeredAt
                      ? new Date(alert.lastTriggeredAt).toLocaleString()
                      : 'Never'}
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => handleToggle(alert)}
                        title={alert.isActive ? 'Pause' : 'Resume'}
                      >
                        {alert.isActive ? <PauseCircle size={14} /> : <PlayCircle size={14} />}
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => handleDelete(alert.id)}
                        title="Delete"
                      >
                        <Trash2 size={13} />
                      </button>
                    </div>
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
