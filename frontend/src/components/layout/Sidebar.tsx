import { NavLink, useNavigate } from 'react-router-dom'
import { LayoutDashboard, GitBranch, Zap, Activity, Settings, LogOut, Shield, CheckSquare, Users, Briefcase, ClipboardList } from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '../../contexts/AuthContext'
import { useRole } from '../../hooks/useRole'
import { useExecutions, useMyTasks } from '../../hooks/useExecutions'

export function Sidebar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const { isManager, isReviewer, canViewWorkflows, canViewAuditLog, canAccessSettings } = useRole()
  const { data: pendingData } = useExecutions({ status: 'WaitingApproval', pageSize: 1 })
  const { data: myTasks } = useMyTasks()

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

  const roleBadgeColor = {
    Manager: 'bg-blue-500',
    Reviewer: 'bg-teal-500',
    Preparer: 'bg-gray-500',
  }[user?.role ?? 'Preparer'] ?? 'bg-gray-500'

  type NavItem = { to: string; label: string; icon: React.ElementType; end?: boolean; badge?: number }

  const navItems: NavItem[] = [
    { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
    ...(canViewWorkflows ? [{ to: '/workflows', label: 'Workflows', icon: GitBranch }] : []),
    ...(isManager ? [{ to: '/automation', label: 'Automation Rules', icon: Zap }] : []),
    { to: '/clients', label: 'Clients', icon: Briefcase },
    { to: '/my-tasks', label: 'My Tasks', icon: ClipboardList, badge: myTasks?.length ?? 0 },
    { to: '/executions', label: 'Executions', icon: Activity },
    { to: '/approvals', label: 'Approvals', icon: CheckSquare, badge: pendingData?.totalCount ?? 0 },
    ...(isManager || isReviewer ? [{ to: '/team', label: 'Team', icon: Users }] : []),
    ...(canViewAuditLog ? [{ to: '/audit', label: 'Audit Log', icon: Shield }] : []),
    ...(canAccessSettings ? [{ to: '/settings', label: 'Settings', icon: Settings }] : []),
  ]

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
        {navItems.map(({ to, label, icon: Icon, end, badge }) => (
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
            <span className="flex-1">{label}</span>
            {!!badge && badge > 0 && (
              <span className="px-1.5 py-0.5 bg-orange-500 text-white text-xs font-bold rounded-full min-w-[20px] text-center">
                {badge}
              </span>
            )}
          </NavLink>
        ))}
      </nav>

      <div className="px-4 py-4 border-t border-gray-700">
        <div className="flex items-center gap-3">
          <div className={`w-8 h-8 rounded-full ${roleBadgeColor} flex items-center justify-center text-xs font-bold flex-shrink-0`}>
            {initials}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{user?.name ?? 'Unknown'}</p>
            <p className="text-xs text-gray-400 truncate">{user?.role ?? ''}</p>
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
