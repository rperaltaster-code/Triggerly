import { api } from './client'
import type { AutomationRule, PagedResult, TriggerType } from '../types'

export const automationRulesApi = {
  list: (params?: { page?: number; pageSize?: number; isEnabled?: boolean }) =>
    api.get<PagedResult<AutomationRule>>('/automationrules', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<AutomationRule>(`/automationrules/${id}`).then((r) => r.data),

  create: (data: { name: string; description: string; triggerType: TriggerType; triggerConfig: string; workflowId: string }) =>
    api.post<AutomationRule>('/automationrules', data).then((r) => r.data),

  update: (id: string, data: { name: string; description: string; triggerConfig: string }) =>
    api.put<AutomationRule>(`/automationrules/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/automationrules/${id}`),

  enable: (id: string) => api.post(`/automationrules/${id}/enable`),

  disable: (id: string) => api.post(`/automationrules/${id}/disable`),
}
