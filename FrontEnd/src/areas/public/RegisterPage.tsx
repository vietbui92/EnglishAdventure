import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { PublicHeader } from '@/areas/public/PublicHeader'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'

// Until the registration API exists (backend S1.4, front end F-R): explains how to get an account meanwhile.
export function RegisterPage() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-svh flex-col bg-muted/40">
      <PublicHeader />
      <main className="flex flex-1 items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>
              <h1 className="font-heading text-xl font-semibold">{t('public.register.title')}</h1>
            </CardTitle>
            <CardDescription>{t('public.register.comingSoon')}</CardDescription>
          </CardHeader>
          <CardContent className="text-sm">
            {t('public.register.haveAccount')}{' '}
            <Link to="/login" className="font-medium underline underline-offset-4">
              {t('public.register.signIn')}
            </Link>
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
