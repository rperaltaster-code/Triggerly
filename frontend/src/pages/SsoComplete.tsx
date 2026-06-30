import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Loader2, Zap, AlertCircle } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'

export function SsoComplete() {
  const [params] = useSearchParams()
  const { loginWithToken } = useAuth()
  const navigate = useNavigate()

  const token = params.get('token')
  const error = params.get('sso_error')

  useEffect(() => {
    if (token) {
      loginWithToken(token)
      navigate('/', { replace: true })
    }
  }, [token, loginWithToken, navigate])

  if (error) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4">
        <div className="w-full max-w-sm text-center">
          <div className="flex items-center justify-center gap-2 mb-8">
            <Zap className="text-blue-400" size={28} />
            <span className="text-2xl font-bold text-white tracking-tight">Triggerly</span>
          </div>
          <div className="bg-red-900/30 border border-red-700 rounded-2xl p-6">
            <AlertCircle className="text-red-400 mx-auto mb-3" size={32} />
            <h2 className="text-white font-semibold mb-2">SSO login failed</h2>
            <p className="text-red-300 text-sm">{decodeURIComponent(error)}</p>
            <button
              onClick={() => navigate('/login')}
              className="mt-4 px-4 py-2 bg-gray-800 text-white text-sm rounded-lg hover:bg-gray-700"
            >
              Back to login
            </button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center">
      <div className="text-center">
        <Loader2 className="animate-spin text-blue-400 mx-auto mb-4" size={32} />
        <p className="text-gray-400 text-sm">Completing sign-in…</p>
      </div>
    </div>
  )
}
