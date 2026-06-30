import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Zap, Loader2, AlertCircle } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'
import { ssoApi } from '../api/sso'
import type { SsoPublicInfo } from '../types'

export function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [params] = useSearchParams()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const ssoError = params.get('sso_error')
  const [error, setError] = useState(ssoError ? decodeURIComponent(ssoError) : '')
  const [loading, setLoading] = useState(false)
  const [ssoInfo, setSsoInfo] = useState<SsoPublicInfo | null>(null)
  const ssoTenantId = params.get('sso') ?? ''
  const [ssoLoading, setSsoLoading] = useState(false)

  useEffect(() => {
    if (!ssoTenantId) return
    ssoApi.getPublicInfo(ssoTenantId)
      .then(info => { if (info.isEnabled) setSsoInfo(info) })
      .catch(() => {})
  }, [ssoTenantId])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid email or password')
    } finally {
      setLoading(false)
    }
  }

  const handleSsoLogin = async () => {
    setSsoLoading(true)
    try {
      const { url } = await ssoApi.getInitUrl(ssoTenantId)
      window.location.href = url
    } catch {
      setError('Could not initiate SSO. Please try again or contact your admin.')
      setSsoLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
      <div className="w-full max-w-sm">
        <div className="flex items-center justify-center gap-2 mb-8">
          <Zap className="text-blue-400" size={28} />
          <span className="text-2xl font-bold text-white tracking-tight">Triggerly</span>
        </div>

        <div className="bg-gray-900 rounded-2xl border border-gray-800 p-8 shadow-xl">
          <h1 className="text-xl font-semibold text-white mb-1">Sign in</h1>
          <p className="text-sm text-gray-400 mb-6">Welcome back to your workspace</p>

          {error && (
            <div className="mb-4 px-3 py-2.5 bg-red-900/40 border border-red-700 rounded-lg text-sm text-red-300 flex items-start gap-2">
              <AlertCircle size={15} className="mt-0.5 shrink-0" />
              {error}
            </div>
          )}

          {ssoInfo && (
            <>
              <button
                onClick={handleSsoLogin}
                disabled={ssoLoading}
                className="w-full py-2.5 mb-4 bg-white text-gray-900 text-sm font-medium rounded-lg hover:bg-gray-100 disabled:opacity-60 flex items-center justify-center gap-2 transition-colors"
              >
                {ssoLoading ? <Loader2 size={15} className="animate-spin" /> : <MicrosoftIcon />}
                {ssoLoading ? 'Redirecting…' : `Sign in with ${ssoInfo.provider}`}
              </button>
              <div className="relative mb-4">
                <div className="absolute inset-0 flex items-center">
                  <div className="w-full border-t border-gray-700" />
                </div>
                <div className="relative flex justify-center">
                  <span className="text-xs text-gray-500 bg-gray-900 px-2">or continue with password</span>
                </div>
              </div>
            </>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus={!ssoInfo}
                placeholder="you@example.com"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Password</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                placeholder="••••••••"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60 flex items-center justify-center gap-2 transition-colors"
            >
              {loading && <Loader2 size={15} className="animate-spin" />}
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
        </div>

        <p className="text-center text-sm text-gray-500 mt-4">
          Don't have an account?{' '}
          <Link to="/register" className="text-blue-400 hover:text-blue-300 font-medium">
            Create one
          </Link>
        </p>
      </div>
    </div>
  )
}

function MicrosoftIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 21 21" fill="none">
      <rect x="1" y="1" width="9" height="9" fill="#F25022"/>
      <rect x="11" y="1" width="9" height="9" fill="#7FBA00"/>
      <rect x="1" y="11" width="9" height="9" fill="#00A4EF"/>
      <rect x="11" y="11" width="9" height="9" fill="#FFB900"/>
    </svg>
  )
}
