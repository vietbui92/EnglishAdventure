import { MailCheck } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { PublicHeader } from '@/areas/public/PublicHeader'
import type { RegisteredLocationState } from '@/areas/public/RegisterPage'
import { ResendConfirmation } from '@/areas/public/ResendConfirmation'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'

/** After registering: explains that a confirmation link was emailed, and offers to send it again. */
export function CheckEmailPage() {
  const { t } = useTranslation()
  const email = (useLocation().state as RegisteredLocationState | null)?.email

  return (
    <div className="flex min-h-svh flex-col bg-muted/40">
      <PublicHeader />
      <main className="flex flex-1 items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardHeader>
            <MailCheck className="mb-2 size-10 text-emerald-600" aria-hidden />
            <CardTitle>
              <h1 className="font-heading text-xl font-semibold">{t('public.checkEmail.title')}</h1>
            </CardTitle>
            <CardDescription>
              {email ? t('public.checkEmail.description', { email }) : t('public.checkEmail.descriptionNoEmail')}
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">{t('public.checkEmail.hint')}</p>
            <ResendConfirmation email={email} />
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
