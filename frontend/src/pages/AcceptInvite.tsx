import { useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { Zap, Loader2, AlertTriangle } from 'lucide-react'
import { authApi } from '../api/auth'

export function AcceptInvite() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [name, setName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (!token) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
        <div className="text-center">
          <AlertTriangle size={40} className="text-red-400 mx-auto mb-4" />
          <p className="text-white text-lg font-semibold">Invalid invite link</p>
          <p className="text-gray-400 text-sm mt-1">This link is missing a token. Please check the email and try again.</p>
          <Link to="/login" className="mt-4 inline-block text-blue-400 hover:text-blue-300 text-sm">Go to login</Link>
        </div>
      </div>
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (password.length < 8) { setError('Password must be at least 8 characters'); return }
    setError('')
    setLoading(true)
    try {
      const { token: jwt, user } = await authApi.acceptInvite(token, name, password)
      // Manually persist auth state then redirect
      localStorage.setItem('triggerly_auth', JSON.stringify({ token: jwt, user }))
      window.location.href = '/'
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept invite. The link may be expired or already used.')
    } finally {
      setLoading(false)
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
          <h1 className="text-xl font-semibold text-white mb-1">Accept invitation</h1>
          <p className="text-sm text-gray-400 mb-6">Create your account to join the team</p>

          {error && (
            <div className="mb-4 px-3 py-2.5 bg-red-900/40 border border-red-700 rounded-lg text-sm text-red-300">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Full name</label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
                placeholder="Jane Smith"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Password</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                placeholder="Min 8 characters"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <button
              type="submit"
              disabled={loading || !name.trim()}
              className="w-full py-2.5 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60 flex items-center justify-center gap-2 transition-colors"
            >
              {loading && <Loader2 size={15} className="animate-spin" />}
              {loading ? 'Setting up account…' : 'Join team'}
            </button>
          </form>
        </div>

        <p className="text-center text-sm text-gray-500 mt-4">
          Already have an account?{' '}
          <Link to="/login" className="text-blue-400 hover:text-blue-300 font-medium">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
