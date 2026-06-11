import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { emailTemplatesApi } from '../api/emailTemplates'

export function useEmailTemplates() {
  return useQuery({
    queryKey: ['email-templates'],
    queryFn: emailTemplatesApi.list,
  })
}

export function useUpsertEmailTemplate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ key, subject, body }: { key: string; subject: string; body: string }) =>
      emailTemplatesApi.upsert(key, subject, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['email-templates'] }),
  })
}

export function useResetEmailTemplate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (key: string) => emailTemplatesApi.reset(key),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['email-templates'] }),
  })
}
