import { X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router'
import { Button } from '@/components/ui/button'

// Full screen, bright theme, large touch targets. Leaving child mode will require the Parent to sign in (F3.2).
export function PlayLayout() {
  const { t } = useTranslation()

  return (
    <div className="theme-play flex min-h-svh flex-col bg-background text-foreground">
      <header className="flex justify-end p-3">
        <Button asChild variant="ghost" className="min-h-12 px-4 text-base">
          <Link to="/">
            <X className="size-5" aria-hidden />
            {t('play.exit')}
          </Link>
        </Button>
      </header>
      <main className="flex flex-1 flex-col items-center justify-center p-4">
        <Outlet />
      </main>
    </div>
  )
}
