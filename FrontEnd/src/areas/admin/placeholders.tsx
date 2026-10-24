import { useTranslation } from 'react-i18next'
import { ComingSoonPage } from '@/shared/components/ComingSoonPage'

// Admin pages that later steps replace: roles (F2.3).

export function AdminDashboardPage() {
  const { t } = useTranslation()
  return (
    <section>
      <h1 className="font-heading text-2xl font-semibold">{t('admin.nav.dashboard')}</h1>
      <p className="mt-2 text-muted-foreground">{t('admin.dashboard.description')}</p>
    </section>
  )
}

export function AdminRolesPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('admin.nav.roles')} />
}
