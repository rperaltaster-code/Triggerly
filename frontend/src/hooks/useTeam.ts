import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { teamApi } from '../api/team'
import type { UserRole } from '../types'

export function useTeam() {
  return useQuery({
    queryKey: ['team'],
    queryFn: teamApi.list,
  })
}

export function useUpdateRole() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: UserRole }) =>
      teamApi.updateRole(userId, role),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['team'] }),
  })
}
