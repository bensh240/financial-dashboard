import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { api } from '../services/api'

const TOKEN_KEY = 'fd_token'

interface AuthContextType {
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  username: string | null
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token,       setToken]       = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [username,    setUsername]    = useState<string | null>(null)
  const [isLoading,   setIsLoading]   = useState(true)

  // On mount, verify token is still valid
  useEffect(() => {
    if (!token) {
      setIsLoading(false)
      return
    }

    api.auth.me()
      .then(data => {
        setUsername(data.username)
      })
      .catch(() => {
        // Token invalid or expired
        localStorage.removeItem(TOKEN_KEY)
        setToken(null)
        setUsername(null)
      })
      .finally(() => setIsLoading(false))
  }, []) // only on mount

  const login = useCallback(async (usernameInput: string, password: string) => {
    const data = await api.auth.login(usernameInput, password)
    localStorage.setItem(TOKEN_KEY, data.token)
    setToken(data.token)
    setUsername(data.username)
  }, [])

  const logout = useCallback(() => {
    api.auth.logout().catch(() => {})
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUsername(null)
  }, [])

  return (
    <AuthContext.Provider value={{
      token,
      isAuthenticated: !!token,
      isLoading,
      username,
      login,
      logout,
    }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
