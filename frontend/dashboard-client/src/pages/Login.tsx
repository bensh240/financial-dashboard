import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { TrendingUp } from 'lucide-react'
import { useAuth } from '../context/AuthContext'

export default function Login() {
  const { login }       = useAuth()
  const navigate        = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error,    setError]    = useState<string | null>(null)
  const [loading,  setLoading]  = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      await login(username, password)
      navigate('/', { replace: true })
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg)',
      padding: 16
    }}>
      <div style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: 12,
        padding: 40,
        width: '100%',
        maxWidth: 380
      }}>
        {/* Logo */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: 10,
          marginBottom: 32,
          justifyContent: 'center'
        }}>
          <TrendingUp size={28} color="var(--accent)" />
          <span style={{ fontSize: 22, fontWeight: 700, color: 'var(--accent)' }}>FinDash</span>
        </div>

        <h1 style={{ fontSize: 20, fontWeight: 700, marginBottom: 8, textAlign: 'center' }}>
          Log In
        </h1>
        <p style={{ color: 'var(--muted)', fontSize: 13, marginBottom: 28, textAlign: 'center' }}>
          Enter your credentials to access the dashboard
        </p>

        {error && <div className="error-banner" style={{ marginBottom: 16 }}>{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Username</label>
            <input
              type="text"
              className="form-input"
              placeholder="admin"
              value={username}
              onChange={e => setUsername(e.target.value)}
              autoFocus
              autoComplete="username"
              required
            />
          </div>

          <div className="form-group" style={{ marginBottom: 24 }}>
            <label className="form-label">Password</label>
            <input
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary w-full"
            disabled={loading}
            style={{ width: '100%', justifyContent: 'center', padding: '10px 16px' }}
          >
            {loading
              ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Logging in…</>
              : 'Log In'}
          </button>
        </form>

        <p style={{ textAlign: 'center', color: 'var(--muted)', fontSize: 12, marginTop: 20 }}>
          Default credentials: <code style={{ color: 'var(--accent)' }}>admin</code> / <code style={{ color: 'var(--accent)' }}>admin123</code>
          <br />
          <span style={{ fontSize: 11 }}>Change them in Settings after logging in.</span>
        </p>
      </div>
    </div>
  )
}
