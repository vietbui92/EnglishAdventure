import { MotionConfig } from 'motion/react'
import { Trophy, Volume2, VolumeX, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, useNavigate } from 'react-router'
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
// the area's sounds, which can be turned off here; animations follow the device's "reduce motion" setting. Pages
// fill the screen (owner 2026-10-24); these buttons float over them in the top right corner.
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
      <div
        className="theme-kids relative flex min-h-svh flex-col bg-background text-foreground"
        onPointerDown={unlockSound}
      >
        <header className="fixed top-0 right-0 z-40 flex gap-2 p-3 sm:p-4">
          <Button
            asChild
            variant="ghost"
            className="min-h-12 rounded-full bg-white/85 px-4 text-base shadow-md backdrop-blur hover:bg-white"
          >
            <Link to="/kids/badges">
              <Trophy className="size-5" aria-hidden />
              <span className="sr-only sm:not-sr-only">{t('kids.rewards.room')}</span>
            </Link>
          </Button>
          <Button
            variant="ghost"
            className="min-h-12 rounded-full bg-white/85 px-4 text-base shadow-md backdrop-blur hover:bg-white"
            aria-pressed={!muted}
            onClick={() => setMuted(!muted)}
          >
            {muted ? <VolumeX className="size-5" aria-hidden /> : <Volume2 className="size-5" aria-hidden />}
            <span className="sr-only sm:not-sr-only">{t('kids.sound')}</span>
          </Button>
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button
                variant="ghost"
                className="min-h-12 rounded-full bg-white/85 px-4 text-base shadow-md backdrop-blur hover:bg-white"
                disabled={leaving}
              >
                <X className="size-5" aria-hidden />
                <span className="sr-only sm:not-sr-only">{t('kids.exit')}</span>
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
        <main className="flex flex-1 flex-col">
          <Outlet />
        </main>
      </div>
    </MotionConfig>
  )
}
