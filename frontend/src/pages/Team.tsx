import { Users } from 'lucide-react'
import { useTeam, useUpdateRole } from '../hooks/useTeam'
import { useRole } from '../hooks/useRole'
import { useAuth } from '../contexts/AuthContext'
import type { UserRole } from '../types'

const ROLES: UserRole[] = ['Preparer', 'Reviewer', 'Manager']

const roleBadge: Record<UserRole, string> = {
  Preparer: 'bg-gray-100 text-gray-600',
  Reviewer: 'bg-teal-100 text-teal-700',
  Manager: 'bg-blue-100 text-blue-700',
}

const roleDescription: Record<UserRole, string> = {
  Preparer: 'Can view and complete assigned tasks',
  Reviewer: 'Can approve steps and view all activity',
  Manager: 'Full access',
}

export function Team() {
  const { user } = useAuth()
  const { isManager } = useRole()
  const { data: members, isLoading } = useTeam()
  const updateRole = useUpdateRole()

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center gap-3">
        <Users size={22} className="text-gray-500" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Team</h1>
          <p className="text-gray-500 text-sm mt-0.5">
            {members?.length ?? 0} member{(members?.length ?? 0) !== 1 ? 's' : ''} in your organisation
          </p>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-800">Members</h2>
          {isManager && (
            <p className="text-sm text-gray-500 mt-0.5">Use the role dropdown to change a member's permissions.</p>
          )}
        </div>

        {isLoading ? (
          <div className="flex justify-center py-10">
            <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-blue-600" />
          </div>
        ) : !members?.length ? (
          <p className="px-6 py-8 text-sm text-gray-400 text-center">No team members found.</p>
        ) : (
          <ul className="divide-y divide-gray-100">
            {members.map((member) => {
              const memberRole = (member.role as UserRole) ?? 'Preparer'
              return (
                <li key={member.userId} className="flex items-center gap-4 px-6 py-4">
                  <div className={`w-9 h-9 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0 ${
                    memberRole === 'Manager' ? 'bg-blue-500' : memberRole === 'Reviewer' ? 'bg-teal-500' : 'bg-gray-400'
                  }`}>
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

                  <div className="flex items-center gap-3 flex-shrink-0">
                    <span className="text-xs text-gray-400 hidden sm:block">
                      {roleDescription[memberRole]}
                    </span>
                    {isManager && member.userId !== user?.id ? (
                      <select
                        value={memberRole}
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
                      <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${roleBadge[memberRole] ?? roleBadge.Preparer}`}>
                        {memberRole}
                      </span>
                    )}
                  </div>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>
  )
}
