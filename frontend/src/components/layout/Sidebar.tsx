import { NavLink } from 'react-router-dom'
import { LayoutDashboard, GitBranch, Zap, Activity, Settings } from 'lucide-react'
import { clsx } from 'clsx'

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/workflows', label: 'Workflows', icon: GitBranch },
  { to: '/automation', label: 'Automation Rules', icon: Zap },
  { to: '/executions', label: 'Executions', icon: Activity },
  { to: '/settings', label: 'Settings', icon: Settings },
]

export function Sidebar() {
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
          <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-xs font-bold">
            T
          </div>
          <div>
            <p className="text-sm font-medium">Demo Tenant</p>
            <p className="text-xs text-gray-400">tenant-demo</p>
          </div>
        </div>
      </div>
    </aside>
  )
}
