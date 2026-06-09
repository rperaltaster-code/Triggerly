import { NavLink, useNavigate } from 'react-router-dom'
import { LayoutDashboard, GitBranch, Zap, Activity, Settings, LogOut, Shield } from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '../../contexts/AuthContext'

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/workflows', label: 'Workflows', icon: GitBranch },
  { to: '/automation', label: 'Automation Rules', icon: Zap },
  { to: '/executions', label: 'Executions', icon: Activity },
  { to: '/audit', label: 'Audit Log', icon: Shield },
  { to: '/settings', label: 'Settings', icon: Settings },
]

export function Sidebar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const initials = user?.name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2) ?? '?'

  return (
    <aside className="w-64 min-h-screen bg-gray-900 text-white flex flex-col">
      <div className="px-6 py-5 border-b border-gray-700">
        <div className="flex items-center gap-2">
          <Zap className="text-blue-400" size={22} />
          <span className="text-lg font-bold tracking-tight">Triggerly</span>
        </div>
        <p className="text-xs text-gray-400 mt-0.5">Workflow Automation</p>
      </div>

      <nav className="flex-1 px-3 py-4 space-y-1">
        {navItems.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) =>
              clsx(
                'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
                isActive
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-300 hover:bg-gray-800 hover:text-white'
              )
            }
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="px-4 py-4 border-t border-gray-700">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-xs font-bold flex-shrink-0">
            {initials}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{user?.name ?? 'Unknown'}</p>
            <p className="text-xs text-gray-400 truncate">{user?.email ?? ''}</p>
          </div>
          <button
            onClick={handleLogout}
            className="p-1.5 text-gray-400 hover:text-white hover:bg-gray-700 rounded-lg transition-colors"
            title="Sign out"
          >
            <LogOut size={15} />
          </button>
        </div>
      </div>
    </aside>
  )
}
