import { api } from './client'
import type { Workflow, WorkflowSummary, WorkflowExecution, WorkflowVersion, PagedResult, WorkflowStatus, FormField } from '../types'

export const workflowsApi = {
  list: (params?: { page?: number; pageSize?: number; status?: WorkflowStatus; search?: string }) =>
    api.get<PagedResult<WorkflowSummary>>('/workflows', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<Workflow>(`/workflows/${id}`).then((r) => r.data),

  create: (data: { name: string; description: string; steps: unknown[] }) =>
    api.post<Workflow>('/workflows', data).then((r) => r.data),

  update: (id: string, data: { name: string; description: string }) =>
    api.put<Workflow>(`/workflows/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/workflows/${id}`),

  activate: (id: string) => api.post(`/workflows/${id}/activate`),

  deactivate: (id: string) => api.post(`/workflows/${id}/deactivate`),

  trigger: (id: string, inputData?: Record<string, unknown>) =>
    api.post<WorkflowExecution>(`/workflows/${id}/trigger`, { inputData }).then((r) => r.data),

  saveSteps: (
    id: string,
    steps: { name: string; type: string; order: number; config: Record<string, unknown>; approverEmail?: string | null }[],
  ) => api.put<Workflow>(`/workflows/${id}/steps`, { steps }).then((r) => r.data),

  saveForm: (id: string, fields: FormField[]) =>
    api.put<Workflow>(`/workflows/${id}/form`, { fields }).then((r) => r.data),

  getVersions: (id: string) =>
    api.get<WorkflowVersion[]>(`/workflows/${id}/versions`).then((r) => r.data),
}
