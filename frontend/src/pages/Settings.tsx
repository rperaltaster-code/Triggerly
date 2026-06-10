import { useTeam, useUpdateRole } from '../hooks/useTeam'
import { useRole } from '../hooks/useRole'
import { useAuth } from '../contexts/AuthContext'
import type { UserRole } from '../types'

const ROLES: UserRole[] = ['Viewer', 'Approver', 'Editor', 'Admin']

const roleDescriptions: Record<UserRole, string> = {
  Viewer: 'Read-only access',
  Approver: 'Can approve / reject executions',
  Editor: 'Can create, edit and trigger workflows',
  Admin: 'Full access including team management',
}

const roleBadge: Record<UserRole, string> = {
  Viewer: 'bg-gray-100 text-gray-600',
  Approver: 'bg-purple-100 text-purple-700',
  Editor: 'bg-blue-100 text-blue-700',
  Admin: 'bg-green-100 text-green-700',
}

export function Settings() {
  const { user } = useAuth()
  const { isAdmin } = useRole()
  const { data: members, isLoading } = useTeam()
  const updateRole = useUpdateRole()

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
        <p className="text-gray-500 mt-1">Manage your team and roles</p>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-800">Team Members</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {isAdmin ? 'Assign roles to control what each member can do.' : 'Your team members and their roles.'}
          </p>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-10">
            <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-blue-600" />
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {members?.map((member) => (
              <li key={member.userId} className="flex items-center gap-4 px-6 py-4">
                <div className="w-9 h-9 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                  {member.name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {member.name}
                    {member.userId === user?.id && (
                      <span className="ml-2 text-xs text-gray-400">(you)</span>
                    )}
                  </p>
                  <p className="text-xs text-gray-500 truncate">{member.email}</p>
                </div>

                {isAdmin && member.userId !== user?.id ? (
                  <select
                    value={member.role}
                    onChange={(e) =>
                      updateRole.mutate({ userId: member.userId, role: e.target.value as UserRole })
                    }
                    className="text-sm border border-gray-200 rounded-lg px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                ) : (
                  <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${roleBadge[member.role]}`}>
                    {member.role}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <h2 className="font-semibold text-gray-800 mb-4">Role Permissions</h2>
        <div className="space-y-3">
          {ROLES.map((r) => (
            <div key={r} className="flex items-center gap-3">
              <span className={`text-xs px-2.5 py-1 rounded-full font-medium w-20 text-center ${roleBadge[r]}`}>
                {r}
              </span>
              <span className="text-sm text-gray-600">{roleDescriptions[r]}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
