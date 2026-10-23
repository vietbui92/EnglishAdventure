import { Gamepad2, ShieldCheck, Users, type LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ApiError } from '@/api/problem-details'
import { useApiStatus } from '@/app/pages/useApiStatus'
import { LanguageSwitcher } from '@/components/LanguageSwitcher'
import { Button } from '@/components/ui/button'

// Temporary development entry page. F1 replaces it with sign-in and a redirect by user type.
export function HomePage() {
  const { t } = useTranslation()
  const apiStatus = useApiStatus()

  const areas: { to: string; icon: LucideIcon; title: string; description: string }[] = [
    { to: '/admin', icon: ShieldCheck, title: t('areas.admin.title'), description: t('areas.admin.description') },
    { to: '/parent', icon: Users, title: t('areas.parent.title'), description: t('areas.parent.description') },
    { to: '/play', icon: Gamepad2, title: t('areas.play.title'), description: t('areas.play.description') },
  ]

  return (
    <main className="mx-auto flex min-h-svh max-w-4xl flex-col gap-8 px-4 py-10">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="font-heading text-3xl font-bold">{t('app.name')}</h1>
          <p className="mt-1 text-muted-foreground">{t('home.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <LanguageSwitcher />
          <Button asChild>
            <Link to="/login">{t('common.signIn')}</Link>
          </Button>
        </div>
      </header>

      <section aria-labelledby="areas-title">
        <h2 id="areas-title" className="mb-4 text-lg font-semibold">
          {t('home.title')}
        </h2>
        <ul className="grid gap-4 sm:grid-cols-3">
          {areas.map(({ to, icon: Icon, title, description }) => (
            <li key={to}>
              <Link
                to={to}
                className="flex h-full flex-col gap-2 rounded-xl border bg-card p-5 transition-colors hover:bg-muted"
              >
                <Icon className="size-6" aria-hidden />
                <span className="font-semibold">{title}</span>
                <span className="text-sm text-muted-foreground">{description}</span>
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <p role="status" className="text-sm text-muted-foreground">
        {apiStatus.isPending && t('home.api.checking')}
        {apiStatus.isSuccess && t('home.api.ready')}
        {apiStatus.isError &&
          t('home.api.error', { kind: apiStatus.error instanceof ApiError ? apiStatus.error.kind : 'network' })}
      </p>
    </main>
  )
}
