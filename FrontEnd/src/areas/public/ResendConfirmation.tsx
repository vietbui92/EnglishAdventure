import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { useResendConfirmation } from '@/areas/public/api'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'

/**
 * Sends a new confirmation link. With a known `email` it is a single button; otherwise it asks for the email.
 * The answer never says whether the email exists.
 */
export function ResendConfirmation({ email }: { email?: string }) {
  const { t } = useTranslation()
  const resend = useResendConfirmation()
  const [typed, setTyped] = useState('')
  const address = email ?? typed.trim()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (address) {
      resend.mutate(address)
    }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3">
      {!email && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="resend-email">{t('public.resend.email')}</Label>
          <Input
            id="resend-email"
            type="email"
            autoComplete="email"
            required
            value={typed}
            onChange={(event) => setTyped(event.target.value)}
          />
        </div>
      )}
      <Button type="submit" variant="outline" className="self-start" disabled={resend.isPending || !address}>
        {resend.isPending ? t('public.resend.submitting') : t('public.resend.submit')}
      </Button>
      {resend.isSuccess && (
        <p role="status" className="text-sm text-muted-foreground">
          {t('public.resend.sent')}
        </p>
      )}
      {resend.isError && (
        <p role="alert" className="text-sm text-destructive">
          {resend.error instanceof ApiError && resend.error.kind === 'rate-limited'
            ? t('public.tooManyAttempts')
            : t('public.resend.failed')}
        </p>
      )}
    </form>
  )
}
