import { ChartLine, Smile } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router'
import { AccountMenu } from '@/shared/components/AccountMenu'
import { AreaNav, type AreaNavItem } from '@/shared/components/AreaNav'
import { Permissions } from '@/shared/auth/permissions'
import { LanguageSwitcher } from '@/shared/components/LanguageSwitcher'

export function ParentLayout() {
  const { t } = useTranslation()
  const items: AreaNavItem[] = [
    {
      to: '/parent/children',
      label: t('parent.nav.children'),
      icon: Smile,
      permission: Permissions.familyChildrenManage,
    },
    {
      to: '/parent/progress',
      label: t('parent.nav.progress'),
      icon: ChartLine,
      permission: Permissions.familyProgressRead,
    },
  ]

  return (
    <div className="flex min-h-svh flex-col">
      <header className="border-b">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
          <Link to="/" className="font-heading font-semibold">
            {t('app.name')}
          </Link>
          <AreaNav items={items} label={t('common.mainNavigation')} className="flex-1 overflow-x-auto" />
          <LanguageSwitcher />
          <AccountMenu />
        </div>
      </header>
      <main className="mx-auto w-full max-w-5xl flex-1 p-4 md:p-6">
        <Outlet />
      </main>
    </div>
  )
}
