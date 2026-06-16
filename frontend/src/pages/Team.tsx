import { useState } from 'react'
import { Users, UserPlus, Trash2, Mail, Clock, BarChart2 } from 'lucide-react'
import { useTeam, useUpdateRole, usePendingInvites, useInviteMember, useRevokeInvite, useTeamWorkload } from '../hooks/useTeam'
import { useRole } from '../hooks/useRole'
import { useAuth } from '../contexts/AuthContext'
import { formatDistanceToNow } from 'date-fns'
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

function InviteModal({ onClose }: { onClose: () => void }) {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<UserRole>('Preparer')
  const [error, setError] = useState('')
  const invite = useInviteMember()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      await invite.mutateAsync({ email, role })
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send invite')
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <div className="flex items-center gap-2 mb-4">
          <UserPlus size={20} className="text-blue-600" />
          <h2 className="text-lg font-semibold text-gray-900">Invite team member</h2>
        </div>

        {error && (
          <div className="mb-4 px-3 py-2.5 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Email address</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoFocus
              placeholder="colleague@example.com"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Role</label>
            <select
              value={role}
              onChange={(e) => setRole(e.target.value as UserRole)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {ROLES.map((r) => (
                <option key={r} value={r}>{r} — {roleDescription[r]}</option>
              ))}
            </select>
          </div>
          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={invite.isPending}
              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50"
            >
              {invite.isPending ? 'Sending…' : 'Send invite'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export function Team() {
  const { user } = useAuth()
  const { isManager } = useRole()
  const { data: members, isLoading } = useTeam()
  const { data: invites } = usePendingInvites()
  const { data: workload } = useTeamWorkload()
  const updateRole = useUpdateRole()
  const revokeInvite = useRevokeInvite()
  const [showInviteModal, setShowInviteModal] = useState(false)

  const maxTasks = Math.max(1, ...(workload?.map((m) => m.openTaskCount) ?? [0]))

  return (
    <>
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users size={22} className="text-gray-500" />
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Team</h1>
            <p className="text-gray-500 text-sm mt-0.5">
              {members?.length ?? 0} member{(members?.length ?? 0) !== 1 ? 's' : ''} in your organisation
            </p>
          </div>
        </div>
        {isManager && (
          <button
            onClick={() => setShowInviteModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
          >
            <UserPlus size={15} /> Invite member
          </button>
        )}
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

      {isManager && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="font-semibold text-gray-800">Pending invites</h2>
            <p className="text-sm text-gray-500 mt-0.5">Invite links expire after 7 days.</p>
          </div>

          {!invites?.length ? (
            <div className="flex items-center gap-3 px-6 py-6 text-sm text-gray-400">
              <Mail size={16} />
              No pending invites.
            </div>
          ) : (
            <ul className="divide-y divide-gray-100">
              {invites.map((invite) => (
                <li key={invite.id} className="flex items-center gap-4 px-6 py-4">
                  <div className="w-9 h-9 rounded-full bg-gray-100 flex items-center justify-center flex-shrink-0">
                    <Mail size={16} className="text-gray-400" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">{invite.email}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${roleBadge[invite.role] ?? roleBadge.Preparer}`}>
                        {invite.role}
                      </span>
                      <span className="text-xs text-gray-400 flex items-center gap-1">
                        <Clock size={11} />
                        Expires {formatDistanceToNow(new Date(invite.expiresAt), { addSuffix: true })}
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={() => revokeInvite.mutate(invite.id)}
                    disabled={revokeInvite.isPending}
                    className="p-1.5 text-red-400 hover:bg-red-50 rounded-lg disabled:opacity-50"
                    title="Revoke invite"
                  >
                    <Trash2 size={15} />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
      {isManager && workload && workload.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center gap-2">
            <BarChart2 size={17} className="text-gray-400" />
            <h2 className="font-semibold text-gray-800">Team Workload</h2>
            <span className="text-xs text-gray-400 ml-1">open tasks per member</span>
          </div>
          <ul className="divide-y divide-gray-100">
            {workload.map((member) => (
              <li key={member.userId} className="flex items-center gap-4 px-6 py-3.5">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold flex-shrink-0 ${
                  member.role === 'Manager' ? 'bg-blue-500' : member.role === 'Reviewer' ? 'bg-teal-500' : 'bg-gray-400'
                }`}>
                  {member.name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)}
                </div>
                <div className="w-32 min-w-0 flex-shrink-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{member.name}</p>
                  <p className="text-xs text-gray-400">{member.role}</p>
                </div>
                <div className="flex-1 flex items-center gap-3">
                  <div className="flex-1 bg-gray-100 rounded-full h-2 overflow-hidden">
                    <div
                      className="h-2 rounded-full bg-blue-500 transition-all"
                      style={{ width: `${(member.openTaskCount / maxTasks) * 100}%` }}
                    />
                  </div>
                  <span className="text-sm font-medium text-gray-700 w-16 text-right flex-shrink-0">
                    {member.openTaskCount} task{member.openTaskCount !== 1 ? 's' : ''}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>

    {showInviteModal && <InviteModal onClose={() => setShowInviteModal(false)} />}
    </>
  )
}
