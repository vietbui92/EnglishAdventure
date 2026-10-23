import { BookOpen, LayoutDashboard, ShieldCheck, Users } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router'
import { AreaNav } from '@/app/layouts/AreaNav'
import { LanguageSwitcher } from '@/components/LanguageSwitcher'

export function AdminLayout() {
  const { t } = useTranslation()
  const items = [
    { to: '/admin', label: t('admin.nav.dashboard'), icon: LayoutDashboard, end: true },
    { to: '/admin/users', label: t('admin.nav.users'), icon: Users },
    { to: '/admin/roles', label: t('admin.nav.roles'), icon: ShieldCheck },
    { to: '/admin/content', label: t('admin.nav.content'), icon: BookOpen },
  ]

  return (
    <div className="flex min-h-svh flex-col md:flex-row">
      <aside className="border-b bg-sidebar md:w-60 md:shrink-0 md:border-r md:border-b-0">
        <div className="flex items-center justify-between px-4 py-3">
          <Link to="/" className="font-heading font-semibold">
            {t('app.name')}
          </Link>
          <span className="text-xs text-muted-foreground">{t('areas.admin.title')}</span>
        </div>
        <AreaNav
          items={items}
          label={t('common.mainNavigation')}
          className="overflow-x-auto px-2 pb-2 md:flex-col md:overflow-visible"
        />
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex justify-end border-b px-4 py-2">
          <LanguageSwitcher />
        </header>
        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
