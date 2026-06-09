import { api } from './client'
import type { PagedResult } from '../types'

export interface AuditLog {
  id: string
  tenantId: string
  userId: string
  userName: string
  action: string
  entityType: string
  entityId: string
  entityName: string
  details: string | null
  timestamp: string
}

export const auditLogsApi = {
  list: (params?: { page?: number; pageSize?: number; entityType?: string; search?: string }) =>
    api.get<PagedResult<AuditLog>>('/auditlogs', { params }).then((r) => r.data),
}
