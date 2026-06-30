import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ssoApi } from '../api/sso'

export function useSsoConfig() {
  return useQuery({
    queryKey: ['sso-config'],
    queryFn: ssoApi.getConfig,
    retry: false,
  })
}

export function useSavesSsoConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ssoApi.saveConfig,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['sso-config'] }),
  })
}

export function useDeleteSsoConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ssoApi.deleteConfig,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['sso-config'] }),
  })
}

export function useToggleSsoConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (enable: boolean) => enable ? ssoApi.enable() : ssoApi.disable(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['sso-config'] }),
  })
}
