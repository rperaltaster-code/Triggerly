import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { authApi } from '../api/auth'
import type { AuthUser } from '../types'

interface AuthState {
  user: AuthUser | null
  token: string | null
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (name: string, email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

const STORAGE_KEY = 'triggerly_auth'

function loadFromStorage(): AuthState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return JSON.parse(raw) as AuthState
  } catch { /* ignore */ }
  return { user: null, token: null }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadFromStorage)

  const persist = useCallback((next: AuthState) => {
    setState(next)
    if (next.token) localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    else localStorage.removeItem(STORAGE_KEY)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const { token, user } = await authApi.login(email, password)
    persist({ token, user })
  }, [persist])

  const register = useCallback(async (name: string, email: string, password: string) => {
    const { token, user } = await authApi.register(name, email, password)
    persist({ token, user })
  }, [persist])

  const logout = useCallback(() => persist({ user: null, token: null }), [persist])

  return (
    <AuthContext.Provider value={{ ...state, isAuthenticated: !!state.token, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
