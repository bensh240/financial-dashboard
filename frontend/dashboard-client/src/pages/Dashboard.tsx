import { useCallback, useEffect, useRef, useState } from 'react'
import { Bell, BellOff, Plus, RefreshCw } from 'lucide-react'
import { api } from '../services/api'
import type { AlertHistory, WatchlistItem } from '../types'
import WatchlistTable from '../components/WatchlistTable'
import StockChart     from '../components/StockChart'
import AddStockModal  from '../components/AddStockModal'

const POLL_MS = Number(import.meta.env.VITE_POLL_INTERVAL_MS ?? 60_000)

export default function Dashboard() {
  const [items,      setItems]      = useState<WatchlistItem[]>([])
  const [selected,   setSelected]   = useState<string | null>(null)
  const [showModal,  setShowModal]  = useState(false)
  const [loading,    setLoading]    = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error,      setError]      = useState<string | null>(null)

  // Push notifications
  const [notifPermission, setNotifPermission] = useState<NotificationPermission>(
    typeof Notification !== 'undefined' ? Notification.permission : 'denied'
  )
  const lastAlertCountRef = useRef<number | null>(null)

  // Request notification permission on mount
  useEffect(() => {
    if (typeof Notification === 'undefined') return
    if (Notification.permission === 'default') {
      Notification.requestPermission().then(p => setNotifPermission(p))
    }
  }, [])

  // Poll alert history and fire push notifications for new alerts
  const checkAlerts = useCallback(async () => {
    if (typeof Notification === 'undefined' || Notification.permission !== 'granted') return

    try {
      const history: AlertHistory[] = await api.alerts.history(undefined, 5)
      const count = history.length

      if (lastAlertCountRef.current !== null && count > lastAlertCountRef.current) {
        const newAlerts = history.slice(0, count - lastAlertCountRef.current)
        for (const alert of newAlerts) {
          new Notification('Price Alert', {
            body: `${alert.symbol}: ${alert.alertType} ${alert.percentChange.toFixed(2)}%`,
            icon: '/favicon.svg'
          })
        }
      }

      lastAlertCountRef.current = count
    } catch {
      // silent – notifications are best-effort
    }
  }, [])

  const fetchWatchlist = useCallback(async (quiet = false) => {
    if (quiet) setRefreshing(true)
    else setLoading(true)
    setError(null)
    try {
      const data = await api.watchlist.list()
      setItems(data)
      if (!selected && data.length > 0) setSelected(data[0].symbol)
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load watchlist')
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }, [selected])

  useEffect(() => {
    fetchWatchlist()
    checkAlerts()

    const id = setInterval(async () => {
      await fetchWatchlist(true)
      await checkAlerts()
    }, POLL_MS)

    return () => clearInterval(id)
  }, [fetchWatchlist, checkAlerts])

  async function handleAdd(symbol: string) {
    await api.watchlist.add(symbol)
    await fetchWatchlist(true)
    setSelected(symbol)
  }

  async function handleRemove(symbol: string) {
    await api.watchlist.remove(symbol)
    setItems(prev => prev.filter(i => i.symbol !== symbol))
    if (selected === symbol) setSelected(items.find(i => i.symbol !== symbol)?.symbol ?? null)
  }

  function toggleNotifications() {
    if (typeof Notification === 'undefined') return
    if (Notification.permission === 'default') {
      Notification.requestPermission().then(p => setNotifPermission(p))
    }
  }

  // Summary stats
  const valid    = items.filter(i => i.percentChange != null)
  const gainers  = valid.filter(i => (i.percentChange ?? 0) > 0).length
  const losers   = valid.filter(i => (i.percentChange ?? 0) < 0).length
  const avgChg   = valid.length ? valid.reduce((s, i) => s + (i.percentChange ?? 0), 0) / valid.length : null

  return (
    <>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <h1 className="page-title">Watchlist</h1>
          {/* Notification badge */}
          {typeof Notification !== 'undefined' && (
            <span
              className={`badge ${notifPermission === 'granted' ? 'badge-green' : 'badge-gray'}`}
              onClick={toggleNotifications}
              style={{ cursor: notifPermission === 'default' ? 'pointer' : 'default', display: 'inline-flex', alignItems: 'center', gap: 4 }}
              title={notifPermission === 'default' ? 'Click to enable notifications' : `Notifications: ${notifPermission}`}
            >
              {notifPermission === 'granted'
                ? <><Bell size={11} /> Notifications: On</>
                : <><BellOff size={11} /> Notifications: Off</>}
            </span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <button className="btn btn-ghost" onClick={() => fetchWatchlist(true)} disabled={refreshing}>
            <RefreshCw size={14} className={refreshing ? 'spin' : ''} />
            Refresh
          </button>
          <button className="btn btn-primary" onClick={() => setShowModal(true)}>
            <Plus size={16} /> Add Stock
          </button>
        </div>
      </div>

      {/* Stats row */}
      <div className="stat-grid">
        <div className="card">
          <div className="card-title">Watchlist Size</div>
          <div className="stat-value">{items.length}</div>
          <div className="stat-label">stocks tracked</div>
        </div>
        <div className="card">
          <div className="card-title">Gainers</div>
          <div className="stat-value positive">{gainers}</div>
          <div className="stat-label">up today</div>
        </div>
        <div className="card">
          <div className="card-title">Losers</div>
          <div className="stat-value negative">{losers}</div>
          <div className="stat-label">down today</div>
        </div>
        <div className="card">
          <div className="card-title">Avg. Change</div>
          <div className={`stat-value ${avgChg == null ? '' : avgChg >= 0 ? 'positive' : 'negative'}`}>
            {avgChg == null ? '—' : `${avgChg >= 0 ? '+' : ''}${avgChg.toFixed(2)}%`}
          </div>
          <div className="stat-label">portfolio average</div>
        </div>
      </div>

      {error   && <div className="error-banner">{error}</div>}

      {loading ? (
        <div className="loading-screen"><div className="spinner" /> Loading watchlist…</div>
      ) : (
        <WatchlistTable
          items={items}
          onRemove={handleRemove}
          onSelect={sym => setSelected(sym === selected ? null : sym)}
          selected={selected}
          refreshing={refreshing}
        />
      )}

      {selected && !loading && (
        <StockChart symbol={selected} />
      )}

      {showModal && (
        <AddStockModal onAdd={handleAdd} onClose={() => setShowModal(false)} />
      )}
    </>
  )
}
