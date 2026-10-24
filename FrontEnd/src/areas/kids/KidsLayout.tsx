import { X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useNavigate } from 'react-router'
import { signOut } from '@/shared/auth/session'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/shared/components/ui/alert-dialog'
import { Button } from '@/shared/components/ui/button'
import '@/areas/kids/kids.css'

// Full screen, bright theme, large touch targets. Leaving signs the device out, so a Parent must sign in to get back
// to the parent pages (PD-010); the confirmation stops accidental taps.
export function KidsLayout() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [leaving, setLeaving] = useState(false)

  const leave = async () => {
    setLeaving(true)
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <div className="theme-kids flex min-h-svh flex-col bg-background text-foreground">
      <header className="flex justify-end p-3">
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="ghost" className="min-h-12 px-4 text-base" disabled={leaving}>
              <X className="size-5" aria-hidden />
              {t('kids.exit')}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('kids.exitTitle')}</AlertDialogTitle>
              <AlertDialogDescription>{t('kids.exitDescription')}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel className="min-h-12">{t('kids.stay')}</AlertDialogCancel>
              <AlertDialogAction variant="outline" className="min-h-12" onClick={() => void leave()}>
                {t('kids.exitConfirm')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </header>
      {/* "Safe" centering: a long page (the map) starts at the top instead of being cut off above it. */}
      <main className="flex flex-1 flex-col items-center justify-center-safe p-4">
        <Outlet />
      </main>
    </div>
  )
}
