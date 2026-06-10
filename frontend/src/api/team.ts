import { api } from './client'
import type { TeamMember, UserRole } from '../types'

export const teamApi = {
  list: () => api.get<TeamMember[]>('/team').then((r) => r.data),
  updateRole: (userId: string, role: UserRole) =>
    api.put(`/team/${userId}/role`, { role }),
}
