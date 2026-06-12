import { api } from './client'
import type { WorkflowExecution, ExecutionComment, PagedResult, ExecutionStatus, MyTask } from '../types'

export const executionsApi = {
  list: (params?: { page?: number; pageSize?: number; workflowId?: string; status?: ExecutionStatus }) =>
    api.get<PagedResult<WorkflowExecution>>('/executions', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<WorkflowExecution>(`/executions/${id}`).then((r) => r.data),

  approve: (id: string) => api.post(`/executions/${id}/approve`),

  reject: (id: string, reason: string) =>
    api.post(`/executions/${id}/reject`, { reason }),

  cancel: (id: string) => api.post(`/executions/${id}/cancel`),

  getComments: (id: string) =>
    api.get<ExecutionComment[]>(`/executions/${id}/comments`).then((r) => r.data),

  addComment: (id: string, content: string) =>
    api.post<ExecutionComment>(`/executions/${id}/comments`, { content }).then((r) => r.data),

  getMyTasks: () =>
    api.get<MyTask[]>('/executions/my-tasks').then((r) => r.data),

  completeStep: (executionId: string, stepId: string) =>
    api.post(`/executions/${executionId}/steps/${stepId}/complete`),

  reassignStep: (executionId: string, stepId: string, newUserId: string) =>
    api.post(`/executions/${executionId}/steps/${stepId}/reassign`, { newUserId }),
}
