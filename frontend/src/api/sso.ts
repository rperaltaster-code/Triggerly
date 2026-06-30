import { api } from './client'
import type { SsoConfig, SsoPublicInfo } from '../types'

export const ssoApi = {
  getPublicInfo: (tenantId: string) =>
    api.get<SsoPublicInfo>(`/sso/${tenantId}/public`).then(r => r.data),

  getInitUrl: (tenantId: string) =>
    api.get<{ url: string }>(`/sso/${tenantId}/init`).then(r => r.data),

  getConfig: () =>
    api.get<SsoConfig>('/sso-config').then(r => r.data),

  saveConfig: (data: {
    clientId: string
    clientSecret: string
    directoryTenantId: string
    groupClaimName: string
    groupRoleMappings: string
  }) => api.put<SsoConfig>('/sso-config', data).then(r => r.data),

  deleteConfig: () => api.delete('/sso-config'),

  enable: () => api.post('/sso-config/enable'),

  disable: () => api.post('/sso-config/disable'),
}
