import { NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard,
  Bell,
  Briefcase,
  History,
  Settings,
  TrendingUp,
  LogOut,
} from 'lucide-react'
import { useAuth } from '../context/AuthContext'

const links = [
  { to: '/',          label: 'Dashboard',     Icon: LayoutDashboard },
  { to: '/portfolio', label: 'Portfolio',     Icon: Briefcase },
  { to: '/alerts',    label: 'Alerts',        Icon: Bell },
  { to: '/history',   label: 'Alert History', Icon: History },
  { to: '/settings',  label: 'Settings',      Icon: Settings },
]

export default function Navigation() {
  const { logout, username } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <TrendingUp size={20} />
        FinDash
      </div>

      <nav style={{ flex: 1 }}>
        {links.map(({ to, label, Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
          >
            <Icon />
            {label}
          </NavLink>
        ))}
      </nav>

      {/* User info + logout */}
      <div style={{
        padding: '16px 20px',
        borderTop: '1px solid var(--border)',
        marginTop: 'auto'
      }}>
        {username && (
          <div style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 8, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {username}
          </div>
        )}
        <button
          onClick={handleLogout}
          className="nav-item"
          style={{
            width: '100%',
            background: 'none',
            border: 'none',
            padding: '8px 0',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            color: 'var(--muted)',
            fontSize: 14,
          }}
        >
          <LogOut size={18} />
          Sign Out
        </button>
      </div>
    </aside>
  )
}
