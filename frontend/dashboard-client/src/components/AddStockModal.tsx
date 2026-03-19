import { useState, useEffect, useRef, type FormEvent } from 'react'
import { X, Search } from 'lucide-react'
import { api } from '../services/api'

interface Props {
  onAdd:   (symbol: string) => Promise<void>
  onClose: () => void
}

interface SearchResult {
  symbol: string
  description: string
}

export default function AddStockModal({ onAdd, onClose }: Props) {
  const [query,    setQuery]    = useState('')
  const [results,  setResults]  = useState<SearchResult[]>([])
  const [selected, setSelected] = useState<SearchResult | null>(null)
  const [loading,  setLoading]  = useState(false)
  const [searching,setSearching]= useState(false)
  const [error,    setError]    = useState<string | null>(null)
  const [open,     setOpen]     = useState(false)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const wrapRef     = useRef<HTMLDivElement>(null)

  // Debounced search
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current)
    if (query.length < 1) { setResults([]); setOpen(false); return }

    debounceRef.current = setTimeout(async () => {
      setSearching(true)
      try {
        const data = await api.stocks.search(query)
        setResults(data)
        setOpen(data.length > 0)
      } catch {
        setResults([])
      } finally {
        setSearching(false)
      }
    }, 300)
  }, [query])

  // Close dropdown on outside click
  useEffect(() => {
    function handler(e: MouseEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node))
        setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  function pick(result: SearchResult) {
    setSelected(result)
    setQuery(`${result.symbol} – ${result.description}`)
    setOpen(false)
    setError(null)
  }

  function handleQueryChange(val: string) {
    setQuery(val)
    setSelected(null) // clear selection on type
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const sym = selected?.symbol ?? query.trim().toUpperCase()
    if (!sym) return

    setLoading(true)
    setError(null)
    try {
      await onAdd(sym)
      onClose()
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to add stock')
    } finally {
      setLoading(false)
    }
  }

  const canSubmit = !loading && (selected != null || query.trim().length > 0)

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>

        {/* Header */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
          <h2 className="modal-title" style={{ margin: 0 }}>Add Stock</h2>
          <button className="btn btn-ghost btn-sm" onClick={onClose}><X size={16} /></button>
        </div>

        {error && <div className="error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group" ref={wrapRef} style={{ position: 'relative' }}>
            <label className="form-label">Search by symbol or company name</label>

            {/* Search input */}
            <div style={{ position: 'relative' }}>
              <Search size={15} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--muted)', pointerEvents: 'none' }} />
              <input
                className="form-input"
                style={{ paddingLeft: 32 }}
                placeholder="e.g. AAPL, ASML.AS, 7203.T"
                value={query}
                onChange={e => handleQueryChange(e.target.value)}
                onFocus={() => results.length > 0 && setOpen(true)}
                autoFocus
                autoComplete="off"
              />
              {searching && (
                <span className="spinner" style={{ position: 'absolute', right: 10, top: '50%', transform: 'translateY(-50%)', width: 14, height: 14 }} />
              )}
            </div>

            {/* Dropdown results */}
            {open && results.length > 0 && (
              <div style={{
                position: 'absolute',
                top: '100%',
                left: 0, right: 0,
                background: 'var(--surface)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius)',
                zIndex: 200,
                marginTop: 4,
                overflow: 'hidden',
                boxShadow: '0 8px 24px rgba(0,0,0,0.4)'
              }}>
                {results.map(r => (
                  <div
                    key={r.symbol}
                    onMouseDown={() => pick(r)}
                    style={{
                      padding: '10px 14px',
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 12,
                      borderBottom: '1px solid var(--border)',
                    }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.05)')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <span style={{ fontWeight: 700, color: 'var(--accent)', minWidth: 60 }}>{r.symbol}</span>
                    <span style={{ color: 'var(--muted)', fontSize: 13, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.description}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {selected && (
            <div style={{
              background: 'rgba(79,142,247,0.1)',
              border: '1px solid rgba(79,142,247,0.3)',
              borderRadius: 'var(--radius)',
              padding: '10px 14px',
              marginBottom: 16,
              fontSize: 13,
              color: 'var(--accent)'
            }}>
              Selected: <strong>{selected.symbol}</strong> — {selected.description}
            </div>
          )}

          <div className="modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={!canSubmit}>
              {loading
                ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Adding…</>
                : 'Add to Watchlist'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
