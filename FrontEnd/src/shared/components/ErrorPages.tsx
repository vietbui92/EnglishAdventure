import { useTranslation } from 'react-i18next'
import { Link, isRouteErrorResponse, useRouteError } from 'react-router'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/** A full-screen message, or a section inside an area layout when `embedded`. */
function ErrorScreen({ title, description, embedded }: { title: string; description: string; embedded?: boolean }) {
  const { t } = useTranslation()
  const Container = embedded ? 'section' : 'main'
  return (
    <Container
      className={cn(
        'flex flex-col items-center justify-center gap-4 p-4 text-center',
        embedded ? 'py-16' : 'min-h-svh',
      )}
    >
      <h1 className="font-heading text-2xl font-semibold">{title}</h1>
      <p className="max-w-md text-muted-foreground">{description}</p>
      <Button asChild>
        <Link to="/">{t('common.backHome')}</Link>
      </Button>
    </Container>
  )
}

export function NotFoundPage() {
  const { t } = useTranslation()
  return <ErrorScreen title={t('errors.notFound.title')} description={t('errors.notFound.description')} />
}

export function ForbiddenPage({ embedded }: { embedded?: boolean }) {
  const { t } = useTranslation()
  return (
    <ErrorScreen
      title={t('errors.forbidden.title')}
      description={t('errors.forbidden.description')}
      embedded={embedded}
    />
  )
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
