import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { api, unwrap } from '@/shared/api/client'

/** The email language follows the interface language. */
function useEmailLanguage() {
  const { i18n } = useTranslation()
  return i18n.resolvedLanguage === 'en' ? 'en' : 'vi'
}

export interface RegisterInput {
  email: string
  password: string
}

/**
 * POST /api/v1/auth/register. Always 202 for a valid request, also when the email is taken (the API never reveals
 * who is registered), so success only means "check your inbox".
 */
export function useRegister() {
  const language = useEmailLanguage()
  return useMutation({
    mutationFn: ({ email, password }: RegisterInput) =>
      unwrap(api.POST('/api/v1/auth/register', { body: { email, password, acceptTerms: true, language } })),
  })
}

/** POST /api/v1/auth/resend-confirmation. Always 202, whatever the email. */
export function useResendConfirmation() {
  const language = useEmailLanguage()
  return useMutation({
    mutationFn: (email: string) => unwrap(api.POST('/api/v1/auth/resend-confirmation', { body: { email, language } })),
  })
}

/**
 * POST /api/v1/auth/confirm-email, run once per token. A query rather than a mutation: React's StrictMode mounts the
 * page twice in development, and a token works only once, so the result must be shared instead of sent twice.
 */
export function useConfirmEmail(token: string) {
  return useQuery({
    queryKey: ['auth', 'confirm-email', token],
    queryFn: async () => {
      await unwrap(api.POST('/api/v1/auth/confirm-email', { body: { token } }))
      return true
    },
    enabled: token.length > 0,
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  })
}
