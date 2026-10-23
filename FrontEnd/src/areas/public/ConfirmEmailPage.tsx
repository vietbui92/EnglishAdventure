import { CircleCheck, CircleX } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { useConfirmEmail } from '@/areas/public/api'
import { PublicHeader } from '@/areas/public/PublicHeader'
import { ResendConfirmation } from '@/areas/public/ResendConfirmation'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'

/** The page the emailed link opens (/confirm-email?token=...): confirms the account, then offers to sign in. */
export function ConfirmEmailPage() {
  const { t } = useTranslation()
  const token = useSearchParams()[0].get('token') ?? ''
  const confirm = useConfirmEmail(token)

  // A missing token and one the API rejects (unknown, used or expired) look the same to the parent.
  const invalid = !token || (confirm.error instanceof ApiError && confirm.error.kind === 'validation')

  return (
    <div className="flex min-h-svh flex-col bg-muted/40">
      <PublicHeader />
      <main className="flex flex-1 items-center justify-center p-4">
        <Card className="w-full max-w-md">
          {invalid ? (
            <>
              <CardHeader>
                <CircleX className="mb-2 size-10 text-destructive" aria-hidden />
                <CardTitle>
                  <h1 className="font-heading text-xl font-semibold">{t('public.confirm.invalidTitle')}</h1>
                </CardTitle>
                <CardDescription>{t('public.confirm.invalidDescription')}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResendConfirmation />
              </CardContent>
            </>
          ) : confirm.isSuccess ? (
            <>
              <CardHeader>
                <CircleCheck className="mb-2 size-10 text-emerald-600" aria-hidden />
                <CardTitle>
                  <h1 className="font-heading text-xl font-semibold">{t('public.confirm.successTitle')}</h1>
                </CardTitle>
                <CardDescription>{t('public.confirm.successDescription')}</CardDescription>
              </CardHeader>
              <CardContent>
                <Button asChild>
                  <Link to="/login">{t('public.confirm.signIn')}</Link>
                </Button>
              </CardContent>
            </>
          ) : confirm.isError ? (
            <CardContent role="alert" className="flex flex-col items-start gap-3 py-6">
              <p>
                {confirm.error instanceof ApiError && confirm.error.kind === 'rate-limited'
                  ? t('public.tooManyAttempts')
                  : t('public.confirm.failed')}
              </p>
              <Button variant="outline" onClick={() => void confirm.refetch()}>
                {t('public.confirm.retry')}
              </Button>
            </CardContent>
          ) : (
            <CardContent className="py-6">
              <p role="status" className="text-muted-foreground">
                {t('public.confirm.checking')}
              </p>
            </CardContent>
          )}
        </Card>
      </main>
    </div>
  )
}
