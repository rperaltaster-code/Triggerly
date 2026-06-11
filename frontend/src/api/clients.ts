import { api } from './client'
import type { Client, ClientSummary, ClientService, ServiceType, FilingPeriod } from '../types'

export interface ClientsListResult {
  items: ClientSummary[]
  totalCount: number
  page: number
  pageSize: number
}

export interface SaveClientPayload {
  name: string
  email: string
  phone?: string | null
  balanceDate?: string | null
  irdNumber?: string | null
  notes?: string | null
}

export interface SaveServiceTypePayload {
  name: string
  description?: string | null
  defaultWorkflowId?: string | null
  defaultFilingPeriod?: FilingPeriod | null
  color?: string | null
}

export interface SaveClientServicePayload {
  serviceTypeId: string
  workflowId: string
  filingPeriod: FilingPeriod
  isActive: boolean
  notes?: string | null
}

export const clientsApi = {
  list: (params?: { page?: number; pageSize?: number; search?: string }) =>
    api.get<ClientsListResult>('/clients', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<Client>(`/clients/${id}`).then((r) => r.data),

  create: (data: SaveClientPayload) =>
    api.post<Client>('/clients', data).then((r) => r.data),

  update: (id: string, data: SaveClientPayload) =>
    api.put<Client>(`/clients/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/clients/${id}`),

  getServices: (clientId: string) =>
    api.get<ClientService[]>(`/clients/${clientId}/services`).then((r) => r.data),

  addService: (clientId: string, data: SaveClientServicePayload) =>
    api.post(`/clients/${clientId}/services`, data).then((r) => r.data),

  updateService: (clientId: string, serviceId: string, data: SaveClientServicePayload) =>
    api.put(`/clients/${clientId}/services/${serviceId}`, data),

  removeService: (clientId: string, serviceId: string) =>
    api.delete(`/clients/${clientId}/services/${serviceId}`),
}

export const serviceTypesApi = {
  list: () =>
    api.get<ServiceType[]>('/service-types').then((r) => r.data),

  create: (data: SaveServiceTypePayload) =>
    api.post<ServiceType>('/service-types', data).then((r) => r.data),

  update: (id: string, data: SaveServiceTypePayload) =>
    api.put<ServiceType>(`/service-types/${id}`, data).then((r) => r.data),

  delete: (id: string) => api.delete(`/service-types/${id}`),
}
