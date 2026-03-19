import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Navigation from './components/Navigation'
import Dashboard    from './pages/Dashboard'
import Alerts       from './pages/Alerts'
import AlertHistory from './pages/AlertHistory'
import Settings     from './pages/Settings'

export default function App() {
  return (
    <BrowserRouter>
      <div className="app-shell">
        <Navigation />
        <main className="main-content">
          <Routes>
            <Route path="/"        element={<Dashboard />} />
            <Route path="/alerts"  element={<Alerts />} />
            <Route path="/history" element={<AlertHistory />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}
