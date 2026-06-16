import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { executionsApi } from '../api/executions'
import type { ExecutionStatus } from '../types'

export const executionKeys = {
  all: ['executions'] as const,
  list: (params?: object) => [...executionKeys.all, 'list', params] as const,
  detail: (id: string) => [...executionKeys.all, id] as const,
}

export function useExecutions(params?: { page?: number; pageSize?: number; workflowId?: string; status?: ExecutionStatus }) {
  return useQuery({
    queryKey: executionKeys.list(params),
    queryFn: () => executionsApi.list(params),
    refetchInterval: 5000,
  })
}

export function useExecution(id: string) {
  return useQuery({
    queryKey: executionKeys.detail(id),
    queryFn: () => executionsApi.getById(id),
    enabled: !!id,
    refetchInterval: 3000,
  })
}

export function useApproveExecution() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: executionsApi.approve,
    onSuccess: () => qc.invalidateQueries({ queryKey: executionKeys.all }),
  })
}

export function useRejectExecution() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => executionsApi.reject(id, reason),
    onSuccess: () => qc.invalidateQueries({ queryKey: executionKeys.all }),
  })
}

export function useCancelExecution() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: executionsApi.cancel,
    onSuccess: () => qc.invalidateQueries({ queryKey: executionKeys.all }),
  })
}

export function useAddComment(executionId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (content: string) => executionsApi.addComment(executionId, content),
    onSuccess: () => qc.invalidateQueries({ queryKey: executionKeys.detail(executionId) }),
  })
}

export function useMyTasks() {
  return useQuery({
    queryKey: ['my-tasks'],
    queryFn: executionsApi.getMyTasks,
    refetchInterval: 10000,
  })
}

export function useCompleteActionStep() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ executionId, stepId }: { executionId: string; stepId: string }) =>
      executionsApi.completeStep(executionId, stepId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-tasks'] })
      qc.invalidateQueries({ queryKey: executionKeys.all })
    },
  })
}

export function useReassignStep() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ executionId, stepId, newUserId }: { executionId: string; stepId: string; newUserId: string }) =>
      executionsApi.reassignStep(executionId, stepId, newUserId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: executionKeys.all })
      qc.invalidateQueries({ queryKey: ['team-workload'] })
    },
  })
}
