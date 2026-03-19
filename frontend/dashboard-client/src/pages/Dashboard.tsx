import { useEffect, useState, useCallback } from 'react'
import { Plus, RefreshCw } from 'lucide-react'
import { api } from '../services/api'
import type { WatchlistItem } from '../types'
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
    const id = setInterval(() => fetchWatchlist(true), POLL_MS)
    return () => clearInterval(id)
  }, [fetchWatchlist])

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

  // Summary stats
  const valid    = items.filter(i => i.percentChange != null)
  const gainers  = valid.filter(i => (i.percentChange ?? 0) > 0).length
  const losers   = valid.filter(i => (i.percentChange ?? 0) < 0).length
  const avgChg   = valid.length ? valid.reduce((s, i) => s + (i.percentChange ?? 0), 0) / valid.length : null

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">Watchlist</h1>
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
