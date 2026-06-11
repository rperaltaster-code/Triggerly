import { api } from './client'
import type { EmailTemplate } from '../types'

export const emailTemplatesApi = {
  list: () => api.get<EmailTemplate[]>('/email-templates').then((r) => r.data),
  upsert: (key: string, subject: string, body: string) =>
    api.put(`/email-templates/${key}`, { subject, body }),
  reset: (key: string) => api.delete(`/email-templates/${key}`),
}
