import { useEffect, useState } from 'react'
import { ExternalLink } from 'lucide-react'
import { api } from '../services/api'
import type { NewsItem, WatchlistItem } from '../types'

const CATEGORIES = [
  { key: 'general',  label: 'General' },
  { key: 'forex',    label: 'Forex' },
  { key: 'crypto',   label: 'Crypto' },
  { key: 'merger',   label: 'M&A' },
]

function NewsCard({ item }: { item: NewsItem }) {
  const date = new Date(item.datetime * 1000).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
  })

  return (
    <a
      href={item.url ?? '#'}
      target="_blank"
      rel="noopener noreferrer"
      style={{ textDecoration: 'none', color: 'inherit', display: 'block' }}
    >
      <div className="card" style={{
        marginBottom: 12,
        display: 'flex',
        gap: 16,
        alignItems: 'flex-start',
        cursor: 'pointer',
        transition: 'border-color 0.15s',
      }}
        onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent)')}
        onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border)')}
      >
        {item.image && (
          <img
            src={item.image}
            alt=""
            style={{ width: 90, height: 60, objectFit: 'cover', borderRadius: 6, flexShrink: 0 }}
            onError={e => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
          />
        )}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontWeight: 600, fontSize: 14, marginBottom: 4, lineHeight: 1.4 }}>
            {item.headline}
          </div>
          {item.summary && (
            <div style={{
              fontSize: 12, color: 'var(--muted)', marginBottom: 6,
              overflow: 'hidden', display: '-webkit-box',
              WebkitLineClamp: 2, WebkitBoxOrient: 'vertical'
            }}>
              {item.summary}
            </div>
          )}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, color: 'var(--muted)' }}>
            {item.source && <span style={{ color: 'var(--accent)', fontWeight: 600 }}>{item.source}</span>}
            <span>{date}</span>
            <ExternalLink size={10} />
          </div>
        </div>
      </div>
    </a>
  )
}

export default function NewsPage() {
  const [tab,       setTab]       = useState<'market' | 'stock'>('market')
  const [category,  setCategory]  = useState('general')
  const [symbol,    setSymbol]    = useState('')
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([])
  const [news,      setNews]      = useState<NewsItem[]>([])
  const [loading,   setLoading]   = useState(false)
  const [error,     setError]     = useState<string | null>(null)

  // Load watchlist for stock picker
  useEffect(() => {
    api.watchlist.list().then(setWatchlist).catch(() => {})
  }, [])

  // Auto-select first watchlist symbol
  useEffect(() => {
    if (watchlist.length > 0 && !symbol) setSymbol(watchlist[0].symbol)
  }, [watchlist, symbol])

  // Fetch news whenever tab/category/symbol changes
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    setNews([])

    const req = tab === 'market'
      ? api.news.market(category)
      : symbol ? api.news.stock(symbol) : Promise.resolve([] as NewsItem[])

    req
      .then(d => { if (!cancelled) setNews(d) })
      .catch(e => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })

    return () => { cancelled = true }
  }, [tab, category, symbol])

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">News</h1>
      </div>

      {/* Tab bar */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        <button
          className={`btn btn-sm ${tab === 'market' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setTab('market')}
        >
          Market News
        </button>
        <button
          className={`btn btn-sm ${tab === 'stock' ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => setTab('stock')}
        >
          Stock News
        </button>
      </div>

      {/* Filters */}
      {tab === 'market' && (
        <div style={{ display: 'flex', gap: 6, marginBottom: 20, flexWrap: 'wrap' }}>
          {CATEGORIES.map(c => (
            <button
              key={c.key}
              className={`btn btn-sm ${category === c.key ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setCategory(c.key)}
            >
              {c.label}
            </button>
          ))}
        </div>
      )}

      {tab === 'stock' && (
        <div style={{ marginBottom: 20, display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
          {watchlist.length > 0 ? (
            watchlist.map(w => (
              <button
                key={w.symbol}
                className={`btn btn-sm ${symbol === w.symbol ? 'btn-primary' : 'btn-ghost'}`}
                onClick={() => setSymbol(w.symbol)}
              >
                {w.symbol}
              </button>
            ))
          ) : (
            <span style={{ fontSize: 13, color: 'var(--muted)' }}>Add stocks to your watchlist to see stock news.</span>
          )}
        </div>
      )}

      {loading && <div className="loading-screen"><div className="spinner" /> Loading news…</div>}
      {error   && <div className="error-banner">{error}</div>}

      {!loading && !error && news.length === 0 && (
        <div className="empty-state"><p>No news found.</p></div>
      )}

      {!loading && news.map(item => <NewsCard key={item.id} item={item} />)}
    </>
  )
}
