import { useAuth } from '../contexts/AuthContext'
import type { UserRole } from '../types'

export function useRole() {
  const { user } = useAuth()
  const role: UserRole = user?.role ?? 'Viewer'

  return {
    role,
    isAdmin: role === 'Admin',
    canEdit: role === 'Admin' || role === 'Editor',
    canApprove: role === 'Admin' || role === 'Approver',
    isViewer: role === 'Viewer',
  }
}
