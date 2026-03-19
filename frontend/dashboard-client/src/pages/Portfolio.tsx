import { useCallback, useEffect, useState } from 'react'
import { Plus, Pencil, Trash2, X, Check } from 'lucide-react'
import { api } from '../services/api'
import type { PortfolioItem, PortfolioSummary, WatchlistItem } from '../types'

function fmt(n: number, decimals = 2) {
  return n.toLocaleString('en-US', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })
}

interface AddFormState {
  symbol: string
  quantity: string
  avgCostPrice: string
  notes: string
}

interface EditState {
  id: number
  quantity: string
  avgCostPrice: string
  notes: string
}

export default function Portfolio() {
  const [items,    setItems]    = useState<PortfolioItem[]>([])
  const [summary,  setSummary]  = useState<PortfolioSummary | null>(null)
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([])
  const [loading,  setLoading]  = useState(true)
  const [error,    setError]    = useState<string | null>(null)
  const [showAdd,  setShowAdd]  = useState(false)
  const [editState, setEditState] = useState<EditState | null>(null)
  const [addForm,   setAddForm]  = useState<AddFormState>({
    symbol: '', quantity: '', avgCostPrice: '', notes: ''
  })
  const [addError,  setAddError]  = useState<string | null>(null)
  const [addLoading, setAddLoading] = useState(false)

  const load = useCallback(async () => {
    try {
      const [itemsData, summaryData, watchlistData] = await Promise.all([
        api.portfolio.list(),
        api.portfolio.summary(),
        api.watchlist.list(),
      ])
      setItems(itemsData)
      setSummary(summaryData)
      setWatchlist(watchlistData)
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load portfolio')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  async function handleAdd() {
    setAddError(null)
    const sym = addForm.symbol.trim().toUpperCase()
    const qty = parseFloat(addForm.quantity)
    const cost = parseFloat(addForm.avgCostPrice)

    if (!sym) { setAddError('Symbol is required'); return }
    if (isNaN(qty) || qty <= 0) { setAddError('Quantity must be a positive number'); return }
    if (isNaN(cost) || cost <= 0) { setAddError('Average cost must be a positive number'); return }

    setAddLoading(true)
    try {
      await api.portfolio.create(sym, qty, cost, addForm.notes || undefined)
      setShowAdd(false)
      setAddForm({ symbol: '', quantity: '', avgCostPrice: '', notes: '' })
      await load()
    } catch (e: unknown) {
      setAddError(e instanceof Error ? e.message : 'Failed to add position')
    } finally {
      setAddLoading(false)
    }
  }

  async function handleEdit(item: PortfolioItem) {
    if (!editState) return
    const qty  = parseFloat(editState.quantity)
    const cost = parseFloat(editState.avgCostPrice)
    if (isNaN(qty) || qty <= 0 || isNaN(cost) || cost <= 0) return

    try {
      await api.portfolio.update(item.id, qty, cost, editState.notes || undefined)
      setEditState(null)
      await load()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to update position')
    }
  }

  async function handleDelete(id: number) {
    if (!confirm('Delete this position?')) return
    try {
      await api.portfolio.delete(id)
      await load()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to delete position')
    }
  }

  if (loading) return <div className="loading-screen"><div className="spinner" /> Loading portfolio…</div>

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">Portfolio</h1>
        <button className="btn btn-primary" onClick={() => { setShowAdd(true); setAddError(null) }}>
          <Plus size={16} /> Add Position
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Summary cards */}
      {summary && (
        <div className="stat-grid" style={{ marginBottom: 28 }}>
          <div className="card">
            <div className="card-title">Total Invested</div>
            <div className="stat-value">${fmt(summary.totalInvested)}</div>
            <div className="stat-label">cost basis</div>
          </div>
          <div className="card">
            <div className="card-title">Current Value</div>
            <div className="stat-value">${fmt(summary.totalCurrentValue)}</div>
            <div className="stat-label">market value</div>
          </div>
          <div className="card">
            <div className="card-title">Total P&L ($)</div>
            <div className={`stat-value ${summary.totalPlDollar >= 0 ? 'positive' : 'negative'}`}>
              {summary.totalPlDollar >= 0 ? '+' : '-'}${fmt(Math.abs(summary.totalPlDollar))}
            </div>
            <div className="stat-label">unrealized gain/loss</div>
          </div>
          <div className="card">
            <div className="card-title">Total P&L (%)</div>
            <div className={`stat-value ${summary.totalPlPercent >= 0 ? 'positive' : 'negative'}`}>
              {summary.totalPlPercent >= 0 ? '+' : ''}{fmt(summary.totalPlPercent)}%
            </div>
            <div className="stat-label">return</div>
          </div>
        </div>
      )}

      {/* Add position modal */}
      {showAdd && (
        <div className="modal-overlay" onClick={() => setShowAdd(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
              <h2 className="modal-title" style={{ margin: 0 }}>Add Position</h2>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowAdd(false)}><X size={16} /></button>
            </div>

            {addError && <div className="error-banner" style={{ marginBottom: 12 }}>{addError}</div>}

            <div className="form-group">
              <label className="form-label">Symbol</label>
              {watchlist.length > 0 ? (
                <select
                  className="form-select"
                  value={addForm.symbol}
                  onChange={e => setAddForm({ ...addForm, symbol: e.target.value })}
                >
                  <option value="">Select a symbol from your watchlist…</option>
                  {watchlist.map(w => (
                    <option key={w.symbol} value={w.symbol}>{w.symbol} — {w.companyName ?? ''}</option>
                  ))}
                </select>
              ) : (
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g. AAPL"
                  value={addForm.symbol}
                  onChange={e => setAddForm({ ...addForm, symbol: e.target.value })}
                />
              )}
            </div>

            <div className="form-group">
              <label className="form-label">Quantity</label>
              <input
                type="number"
                className="form-input"
                placeholder="e.g. 10"
                value={addForm.quantity}
                onChange={e => setAddForm({ ...addForm, quantity: e.target.value })}
                min="0.0001"
                step="any"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Avg. Cost Price ($)</label>
              <input
                type="number"
                className="form-input"
                placeholder="e.g. 150.00"
                value={addForm.avgCostPrice}
                onChange={e => setAddForm({ ...addForm, avgCostPrice: e.target.value })}
                min="0.0001"
                step="any"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Notes (optional)</label>
              <input
                type="text"
                className="form-input"
                placeholder="e.g. Long-term hold"
                value={addForm.notes}
                onChange={e => setAddForm({ ...addForm, notes: e.target.value })}
              />
            </div>

            <div className="modal-actions">
              <button className="btn btn-ghost" onClick={() => setShowAdd(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleAdd} disabled={addLoading}>
                {addLoading ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Adding…</> : 'Add Position'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Portfolio table */}
      {items.length === 0 ? (
        <div className="empty-state">
          <h3>No Positions Yet</h3>
          <p>Add a stock position to start tracking your portfolio performance.</p>
        </div>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Quantity</th>
                <th>Avg Cost</th>
                <th>Current Price</th>
                <th>Value</th>
                <th>P&L ($)</th>
                <th>P&L (%)</th>
                <th>Notes</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => {
                const isEditing = editState?.id === item.id
                return (
                  <tr key={item.id}>
                    <td style={{ fontWeight: 700 }}>{item.symbol}</td>
                    <td>
                      {isEditing ? (
                        <input
                          type="number"
                          className="form-input"
                          style={{ width: 80, padding: '4px 8px' }}
                          value={editState.quantity}
                          onChange={e => setEditState({ ...editState, quantity: e.target.value })}
                          step="any"
                        />
                      ) : fmt(item.quantity, 4).replace(/\.?0+$/, '')}
                    </td>
                    <td>
                      {isEditing ? (
                        <input
                          type="number"
                          className="form-input"
                          style={{ width: 100, padding: '4px 8px' }}
                          value={editState.avgCostPrice}
                          onChange={e => setEditState({ ...editState, avgCostPrice: e.target.value })}
                          step="any"
                        />
                      ) : `$${fmt(item.avgCostPrice)}`}
                    </td>
                    <td>${fmt(item.currentPrice)}</td>
                    <td>${fmt(item.currentValue)}</td>
                    <td className={item.plDollar >= 0 ? 'positive' : 'negative'}>
                      {item.plDollar >= 0 ? '+' : '-'}${fmt(Math.abs(item.plDollar))}
                    </td>
                    <td className={item.plPercent >= 0 ? 'positive' : 'negative'}>
                      {item.plPercent >= 0 ? '+' : ''}{fmt(item.plPercent)}%
                    </td>
                    <td>
                      {isEditing ? (
                        <input
                          type="text"
                          className="form-input"
                          style={{ width: 120, padding: '4px 8px' }}
                          value={editState.notes}
                          onChange={e => setEditState({ ...editState, notes: e.target.value })}
                        />
                      ) : (
                        <span style={{ color: 'var(--muted)', fontSize: 12 }}>{item.notes ?? '—'}</span>
                      )}
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 6 }}>
                        {isEditing ? (
                          <>
                            <button
                              className="btn btn-sm btn-primary"
                              onClick={() => handleEdit(item)}
                              title="Save"
                            >
                              <Check size={13} />
                            </button>
                            <button
                              className="btn btn-sm btn-ghost"
                              onClick={() => setEditState(null)}
                              title="Cancel"
                            >
                              <X size={13} />
                            </button>
                          </>
                        ) : (
                          <>
                            <button
                              className="btn btn-sm btn-ghost"
                              onClick={() => setEditState({
                                id: item.id,
                                quantity: String(item.quantity),
                                avgCostPrice: String(item.avgCostPrice),
                                notes: item.notes ?? ''
                              })}
                              title="Edit"
                            >
                              <Pencil size={13} />
                            </button>
                            <button
                              className="btn btn-sm btn-danger"
                              onClick={() => handleDelete(item.id)}
                              title="Delete"
                            >
                              <Trash2 size={13} />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
