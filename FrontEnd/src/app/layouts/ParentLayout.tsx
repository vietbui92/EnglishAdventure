import { ChartLine, Smile } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router'
import { AreaNav } from '@/app/layouts/AreaNav'
import { LanguageSwitcher } from '@/components/LanguageSwitcher'

export function ParentLayout() {
  const { t } = useTranslation()
  const items = [
    { to: '/parent/children', label: t('parent.nav.children'), icon: Smile },
    { to: '/parent/progress', label: t('parent.nav.progress'), icon: ChartLine },
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
        </div>
      </header>
      <main className="mx-auto w-full max-w-5xl flex-1 p-4 md:p-6">
        <Outlet />
      </main>
    </div>
  )
}
