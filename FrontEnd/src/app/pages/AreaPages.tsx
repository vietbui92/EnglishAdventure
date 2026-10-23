import { useTranslation } from 'react-i18next'
import { ComingSoonPage } from '@/app/pages/ComingSoonPage'
import { Button } from '@/components/ui/button'

// Placeholder pages for the area shells (F0.4). Each is replaced by its feature step.

export function AdminDashboardPage() {
  const { t } = useTranslation()
  return (
    <section>
      <h1 className="font-heading text-2xl font-semibold">{t('admin.nav.dashboard')}</h1>
      <p className="mt-2 text-muted-foreground">{t('admin.dashboard.description')}</p>
    </section>
  )
}

export function AdminUsersPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('admin.nav.users')} />
}

export function AdminRolesPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('admin.nav.roles')} />
}

export function AdminContentPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('admin.nav.content')} />
}

export function ParentChildrenPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('parent.nav.children')} />
}

export function ParentProgressPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('parent.nav.progress')} />
}

export function PlayHomePage() {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col items-center gap-6 text-center">
      <h1 className="font-heading text-4xl font-bold">{t('play.title')}</h1>
      <p className="text-muted-foreground">{t('play.subtitle')}</p>
      <Button className="min-h-14 rounded-2xl px-10 text-xl" disabled>
        {t('play.start')}
      </Button>
    </div>
  )
}
