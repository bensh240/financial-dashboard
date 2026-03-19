import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import Navigation   from './components/Navigation'
import Dashboard    from './pages/Dashboard'
import Alerts       from './pages/Alerts'
import AlertHistory from './pages/AlertHistory'
import Settings     from './pages/Settings'
import Portfolio    from './pages/Portfolio'
import Login        from './pages/Login'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="loading-screen" style={{ minHeight: '100vh' }}>
        <div className="spinner" /> Loading…
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function AppShell() {
  return (
    <div className="app-shell">
      <Navigation />
      <main className="main-content">
        <Routes>
          <Route path="/"          element={<Dashboard />} />
          <Route path="/alerts"    element={<Alerts />} />
          <Route path="/history"   element={<AlertHistory />} />
          <Route path="/settings"  element={<Settings />} />
          <Route path="/portfolio" element={<Portfolio />} />
        </Routes>
      </main>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <AppShell />
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
