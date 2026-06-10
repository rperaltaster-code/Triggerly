import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { workflowsApi } from '../api/workflows'
import type { WorkflowStatus } from '../types'

export const workflowKeys = {
  all: ['workflows'] as const,
  list: (params?: object) => [...workflowKeys.all, 'list', params] as const,
  detail: (id: string) => [...workflowKeys.all, id] as const,
}

export function useWorkflows(params?: { page?: number; pageSize?: number; status?: WorkflowStatus; search?: string }) {
  return useQuery({
    queryKey: workflowKeys.list(params),
    queryFn: () => workflowsApi.list(params),
  })
}

export function useWorkflow(id: string) {
  return useQuery({
    queryKey: workflowKeys.detail(id),
    queryFn: () => workflowsApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: workflowsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: workflowKeys.all }),
  })
}

export function useUpdateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; description: string } }) =>
      workflowsApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: workflowKeys.all }),
  })
}

export function useDeleteWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: workflowsApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: workflowKeys.all }),
  })
}

export function useActivateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: workflowsApi.activate,
    onSuccess: () => qc.invalidateQueries({ queryKey: workflowKeys.all }),
  })
}

export function useTriggerWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, inputData }: { id: string; inputData?: Record<string, unknown> }) =>
      workflowsApi.trigger(id, inputData),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['executions'] }),
  })
}

export function useSaveWorkflowForm() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, fields }: { id: string; fields: import('../types').FormField[] }) =>
      workflowsApi.saveForm(id, fields),
    onSuccess: (_data, { id }) => qc.invalidateQueries({ queryKey: workflowKeys.detail(id) }),
  })
}
