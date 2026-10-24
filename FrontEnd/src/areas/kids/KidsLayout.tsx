import { MotionConfig } from 'motion/react'
import { Volume2, VolumeX, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useNavigate } from 'react-router'
import { setMuted, unlockSound, useMuted } from '@/areas/kids/adventure/sound'
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
// to the parent pages (PD-010); the confirmation stops accidental taps. The first tap anywhere lets the browser play
// the area's sounds, which can be turned off here; animations follow the device's "reduce motion" setting.
export function KidsLayout() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [leaving, setLeaving] = useState(false)
  const muted = useMuted()

  const leave = async () => {
    setLeaving(true)
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <MotionConfig reducedMotion="user">
      <div className="theme-kids flex min-h-svh flex-col bg-background text-foreground" onPointerDown={unlockSound}>
        <header className="flex justify-end gap-2 p-3">
          <Button
            variant="ghost"
            className="min-h-12 px-4 text-base"
            aria-pressed={!muted}
            onClick={() => setMuted(!muted)}
          >
            {muted ? <VolumeX className="size-5" aria-hidden /> : <Volume2 className="size-5" aria-hidden />}
            {t('kids.sound')}
          </Button>
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
    </MotionConfig>
  )
}
