import { api } from './client'
import type { WorkflowExecution, PagedResult, ExecutionStatus } from '../types'

export const executionsApi = {
  list: (params?: { page?: number; pageSize?: number; workflowId?: string; status?: ExecutionStatus }) =>
    api.get<PagedResult<WorkflowExecution>>('/executions', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<WorkflowExecution>(`/executions/${id}`).then((r) => r.data),

  approve: (id: string) => api.post(`/executions/${id}/approve`),

  reject: (id: string, reason: string) =>
    api.post(`/executions/${id}/reject`, { reason }),

  cancel: (id: string) => api.post(`/executions/${id}/cancel`),
}
