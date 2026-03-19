import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Bell,
  History,
  Settings,
  TrendingUp,
} from 'lucide-react'

const links = [
  { to: '/',         label: 'Dashboard',     Icon: LayoutDashboard },
  { to: '/alerts',   label: 'Alerts',        Icon: Bell },
  { to: '/history',  label: 'Alert History', Icon: History },
  { to: '/settings', label: 'Settings',      Icon: Settings },
]

export default function Navigation() {
  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <TrendingUp size={20} />
        FinDash
      </div>

      <nav>
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
    </aside>
  )
}
