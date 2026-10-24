import { Award, BookOpen, LayoutDashboard, ShieldCheck, Users } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router'
import { AccountMenu } from '@/shared/components/AccountMenu'
import { AreaNav, type AreaNavItem } from '@/shared/components/AreaNav'
import { Permissions } from '@/shared/auth/permissions'
import { LanguageSwitcher } from '@/shared/components/LanguageSwitcher'

export function AdminLayout() {
  const { t } = useTranslation()
  const items: AreaNavItem[] = [
    { to: '/admin', label: t('admin.nav.dashboard'), icon: LayoutDashboard, end: true },
    { to: '/admin/users', label: t('admin.nav.users'), icon: Users, permission: Permissions.usersRead },
    { to: '/admin/roles', label: t('admin.nav.roles'), icon: ShieldCheck, permission: Permissions.rolesManage },
    { to: '/admin/content', label: t('admin.nav.content'), icon: BookOpen, permission: Permissions.contentReadDrafts },
    { to: '/admin/rewards', label: t('admin.nav.rewards'), icon: Award, permission: Permissions.rewardsManage },
  ]

  return (
    <div className="flex min-h-svh flex-col md:flex-row">
      <aside className="border-b bg-sidebar md:w-60 md:shrink-0 md:border-r md:border-b-0">
        <div className="flex items-center justify-between px-4 py-3">
          <Link to="/" className="font-heading font-semibold">
            {t('app.name')}
          </Link>
          <span className="text-xs text-muted-foreground">{t('admin.title')}</span>
        </div>
        <AreaNav
          items={items}
          label={t('common.mainNavigation')}
          className="overflow-x-auto px-2 pb-2 md:flex-col md:overflow-visible"
        />
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex flex-wrap items-center justify-end gap-x-4 gap-y-2 border-b px-4 py-2">
          <LanguageSwitcher />
          <AccountMenu />
        </header>
        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
