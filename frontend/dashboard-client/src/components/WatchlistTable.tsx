import { Trash2 } from 'lucide-react'
import type { WatchlistItem } from '../types'

interface Props {
  items:      WatchlistItem[]
  onRemove:   (symbol: string) => void
  onSelect:   (symbol: string) => void
  selected:   string | null
  refreshing: boolean
}

function fmt(n: number | null | undefined, prefix = '', decimals = 2): string {
  if (n == null) return '—'
  return `${prefix}${n.toFixed(decimals)}`
}

function PctBadge({ v }: { v: number | null }) {
  if (v == null) return <span className="neutral">—</span>
  const cls = v > 0 ? 'positive' : v < 0 ? 'negative' : 'neutral'
  const sign = v > 0 ? '+' : ''
  return <span className={cls}>{sign}{v.toFixed(2)}%</span>
}

export default function WatchlistTable({ items, onRemove, onSelect, selected, refreshing }: Props) {
  if (items.length === 0) {
    return (
      <div className="empty-state">
        <h3>Your watchlist is empty</h3>
        <p>Add a stock symbol to get started.</p>
      </div>
    )
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Symbol</th>
            <th>Company</th>
            <th>Price</th>
            <th>Change</th>
            <th>% Change</th>
            <th>Prev. Close</th>
            <th>Last Updated</th>
            <th style={{ width: 60 }}>
              {refreshing && <span className="spinner" style={{ width: 14, height: 14 }} />}
            </th>
          </tr>
        </thead>
        <tbody>
          {items.map(item => (
            <tr
              key={item.id}
              onClick={() => onSelect(item.symbol)}
              style={{ cursor: 'pointer', background: selected === item.symbol ? 'rgba(79,142,247,0.07)' : undefined }}
            >
              <td>
                <span style={{ fontWeight: 700, color: 'var(--accent)' }}>{item.symbol}</span>
              </td>
              <td style={{ color: 'var(--muted)', maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                {item.companyName ?? '—'}
              </td>
              <td style={{ fontWeight: 600 }}>
                {fmt(item.price, '$')}
              </td>
              <td>
                {item.change == null ? <span className="neutral">—</span> : (
                  <span className={item.change >= 0 ? 'positive' : 'negative'}>
                    {item.change >= 0 ? '+' : ''}{fmt(item.change, '$')}
                  </span>
                )}
              </td>
              <td><PctBadge v={item.percentChange} /></td>
              <td style={{ color: 'var(--muted)' }}>{fmt(item.previousClose, '$')}</td>
              <td style={{ color: 'var(--muted)', fontSize: 12 }}>
                {item.lastUpdatedAt
                  ? new Date(item.lastUpdatedAt).toLocaleTimeString()
                  : '—'}
              </td>
              <td>
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={e => { e.stopPropagation(); onRemove(item.symbol) }}
                  title="Remove"
                >
                  <Trash2 size={13} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
