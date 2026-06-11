import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { clientsApi, serviceTypesApi } from '../api/clients'
import type { SaveClientPayload, SaveServiceTypePayload, SaveClientServicePayload } from '../api/clients'

export function useClients(params?: { page?: number; pageSize?: number; search?: string }) {
  return useQuery({
    queryKey: ['clients', params],
    queryFn: () => clientsApi.list(params),
  })
}

export function useClient(id: string | undefined) {
  return useQuery({
    queryKey: ['clients', id],
    queryFn: () => clientsApi.getById(id!),
    enabled: !!id,
  })
}

export function useClientServices(clientId: string | undefined) {
  return useQuery({
    queryKey: ['client-services', clientId],
    queryFn: () => clientsApi.getServices(clientId!),
    enabled: !!clientId,
  })
}

export function useServiceTypes() {
  return useQuery({
    queryKey: ['service-types'],
    queryFn: () => serviceTypesApi.list(),
  })
}

export function useCreateClient() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveClientPayload) => clientsApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['clients'] }),
  })
}

export function useUpdateClient(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveClientPayload) => clientsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['clients'] })
      qc.invalidateQueries({ queryKey: ['clients', id] })
    },
  })
}

export function useDeleteClient() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => clientsApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['clients'] }),
  })
}

export function useAddClientService(clientId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveClientServicePayload) => clientsApi.addService(clientId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['client-services', clientId] }),
  })
}

export function useUpdateClientService(clientId: string, serviceId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveClientServicePayload) => clientsApi.updateService(clientId, serviceId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['client-services', clientId] }),
  })
}

export function useRemoveClientService(clientId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (serviceId: string) => clientsApi.removeService(clientId, serviceId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['client-services', clientId] }),
  })
}

export function useCreateServiceType() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveServiceTypePayload) => serviceTypesApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-types'] }),
  })
}

export function useUpdateServiceType(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: SaveServiceTypePayload) => serviceTypesApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-types'] }),
  })
}

export function useDeleteServiceType() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => serviceTypesApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['service-types'] }),
  })
}
