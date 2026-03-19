import { useEffect, useState, type FormEvent } from 'react'
import { Save, CheckCircle } from 'lucide-react'
import { api } from '../services/api'
import type { Settings } from '../types'

export default function SettingsPage() {
  const [settings, setSettings] = useState<Settings | null>(null)
  const [loading,  setLoading]  = useState(true)
  const [saving,   setSaving]   = useState(false)
  const [saved,    setSaved]    = useState(false)
  const [error,    setError]    = useState<string | null>(null)

  useEffect(() => {
    api.settings.get()
      .then(setSettings)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  async function handleSave(e: FormEvent) {
    e.preventDefault()
    if (!settings) return
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      const updated = await api.settings.update(settings)
      setSettings(updated)
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save settings')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="loading-screen"><div className="spinner" /> Loading settings…</div>
  if (!settings) return <div className="error-banner">{error ?? 'Could not load settings'}</div>

  return (
    <>
      <div className="page-header">
        <h1 className="page-title">Settings</h1>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <form onSubmit={handleSave} style={{ maxWidth: 540 }}>

        {/* Email Notifications */}
        <div className="card" style={{ marginBottom: 20 }}>
          <div className="card-title">Email Notifications</div>

          <div className="form-group">
            <label className="form-label">Recipient Email</label>
            <input
              type="email"
              className="form-input"
              placeholder="you@example.com"
              value={settings.email}
              onChange={e => setSettings({ ...settings, email: e.target.value })}
            />
            <span style={{ fontSize: 12, color: 'var(--muted)' }}>
              Alert emails and reports will be sent to this address.
            </span>
          </div>
        </div>

        {/* Daily Brief */}
        <div className="card" style={{ marginBottom: 20 }}>
          <div className="card-title">Daily Brief</div>

          <label className="form-toggle">
            <span className="toggle">
              <input
                type="checkbox"
                checked={settings.dailyBriefEnabled}
                onChange={e => setSettings({ ...settings, dailyBriefEnabled: e.target.checked })}
              />
              <span className="toggle-slider" />
            </span>
            <span>Enable daily morning brief email</span>
          </label>

          <div className="form-group">
            <label className="form-label">Send Time (UTC)</label>
            <input
              type="time"
              className="form-input"
              value={settings.dailyBriefTime}
              onChange={e => setSettings({ ...settings, dailyBriefTime: e.target.value })}
              disabled={!settings.dailyBriefEnabled}
            />
            <span style={{ fontSize: 12, color: 'var(--muted)' }}>
              The Hangfire cron job will be rescheduled immediately after saving.
            </span>
          </div>
        </div>

        {/* Weekly Report */}
        <div className="card" style={{ marginBottom: 20 }}>
          <div className="card-title">Weekly Report</div>

          <label className="form-toggle">
            <span className="toggle">
              <input
                type="checkbox"
                checked={settings.weeklyReportEnabled}
                onChange={e => setSettings({ ...settings, weeklyReportEnabled: e.target.checked })}
              />
              <span className="toggle-slider" />
            </span>
            <span>Enable weekly performance report (Sundays 9 AM UTC)</span>
          </label>
        </div>

        {/* Monitoring */}
        <div className="card" style={{ marginBottom: 24 }}>
          <div className="card-title">Price Monitoring</div>

          <div className="form-group">
            <label className="form-label">Volatility Threshold (%)</label>
            <input
              type="number"
              className="form-input"
              value={settings.volatilityThreshold}
              onChange={e => setSettings({ ...settings, volatilityThreshold: Number(e.target.value) })}
              min="0.1"
              max="100"
              step="0.1"
            />
            <span style={{ fontSize: 12, color: 'var(--muted)' }}>
              Trigger a VOLATILITY alert when a stock swings this % within a single 5-minute polling cycle.
            </span>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Saving…</> : <><Save size={14} /> Save Settings</>}
          </button>
          {saved && (
            <span style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--green)', fontSize: 13 }}>
              <CheckCircle size={14} /> Saved successfully
            </span>
          )}
        </div>
      </form>
    </>
  )
}
