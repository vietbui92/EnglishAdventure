import { useTranslation } from 'react-i18next'
import { Link, isRouteErrorResponse, useRouteError } from 'react-router'
import { Button } from '@/components/ui/button'

function ErrorScreen({ title, description }: { title: string; description: string }) {
  const { t } = useTranslation()
  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-4 p-4 text-center">
      <h1 className="font-heading text-2xl font-semibold">{title}</h1>
      <p className="max-w-md text-muted-foreground">{description}</p>
      <Button asChild>
        <Link to="/">{t('common.backHome')}</Link>
      </Button>
    </main>
  )
}

export function NotFoundPage() {
  const { t } = useTranslation()
  return <ErrorScreen title={t('errors.notFound.title')} description={t('errors.notFound.description')} />
}

/** Shown when a route throws while rendering or loading. */
export function RouteErrorPage() {
  const error = useRouteError()
  const { t } = useTranslation()

  if (isRouteErrorResponse(error) && error.status === 404) {
    return <NotFoundPage />
  }
  return <ErrorScreen title={t('errors.unexpected.title')} description={t('errors.unexpected.description')} />
}
