import { useAuth } from '../contexts/AuthContext'
import type { UserRole } from '../types'

export function useRole() {
  const { user } = useAuth()
  const role: UserRole = (user?.role as UserRole) ?? 'Preparer'

  const isManager = role === 'Manager'
  const isReviewer = role === 'Reviewer'
  const isPreparer = role === 'Preparer'

  return {
    role,
    isManager,
    isReviewer,
    isPreparer,
    // Execution permissions
    canTrigger: isManager || isReviewer,
    canCancel: isManager || isReviewer,
    canApprove: isManager || isReviewer,
    // Workflow permissions
    canViewWorkflows: isManager || isReviewer,
    canEditWorkflows: isManager,
    // Client permissions
    canManageClients: isManager,
    // Team permissions
    canViewTeam: isManager || isReviewer,
    canManageTeam: isManager,
    // Settings
    canAccessSettings: isManager,
    // Audit log
    canViewAuditLog: isManager || isReviewer,
    // Legacy aliases kept for compatibility during rollout
    isAdmin: isManager,
    canEdit: isManager,
  }
}
