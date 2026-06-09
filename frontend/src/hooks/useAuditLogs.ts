import { useQuery } from '@tanstack/react-query'
import { auditLogsApi } from '../api/auditLogs'

export function useAuditLogs(params?: { page?: number; pageSize?: number; entityType?: string; search?: string }) {
  return useQuery({
    queryKey: ['audit-logs', params],
    queryFn: () => auditLogsApi.list(params),
    refetchInterval: 15_000,
  })
}
