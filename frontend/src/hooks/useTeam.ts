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

export function usePendingInvites() {
  return useQuery({
    queryKey: ['team-invites'],
    queryFn: teamApi.listInvites,
  })
}

export function useInviteMember() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ email, role }: { email: string; role: UserRole }) =>
      teamApi.invite(email, role),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['team-invites'] }),
  })
}

export function useRevokeInvite() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => teamApi.revokeInvite(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['team-invites'] }),
  })
}

export function useTeamWorkload() {
  return useQuery({
    queryKey: ['team-workload'],
    queryFn: teamApi.getWorkload,
  })
}
