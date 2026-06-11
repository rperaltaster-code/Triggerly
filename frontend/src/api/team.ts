import { api } from './client'
import type { TeamInvite, TeamMember, UserRole } from '../types'

export const teamApi = {
  list: () => api.get<TeamMember[]>('/team').then((r) => r.data),
  updateRole: (userId: string, role: UserRole) =>
    api.put(`/team/${userId}/role`, { role }),
  invite: (email: string, role: UserRole) =>
    api.post('/team/invite', { email, role }),
  listInvites: () => api.get<TeamInvite[]>('/team/invites').then((r) => r.data),
  revokeInvite: (id: string) => api.delete(`/team/invites/${id}`),
}
