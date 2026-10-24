import { useMutation } from '@tanstack/react-query'
import { Play } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import type { ChildProfile } from '@/areas/parent/children/api'
import { startChildSession } from '@/shared/auth/session'
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

/**
 * "Let them learn": switches this device to the child's learning area (S2.2, PD-010) after the Parent confirms that
 * coming back will need their sign-in.
 */
export function StartLearningButton({ profile }: { profile: ChildProfile }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const start = useMutation({
    mutationFn: () => startChildSession(profile.id),
    onSuccess: () => navigate('/kids', { replace: true }),
  })

  return (
    <div className="flex flex-col items-center gap-1">
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button disabled={start.isPending}>
            <Play aria-hidden />
            {t('parent.children.startLearning')}
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('parent.children.startTitle', { nickname: profile.nickname })}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('parent.children.startDescription', { nickname: profile.nickname })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('parent.children.cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={() => start.mutate()}>{t('parent.children.start')}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      {start.isError && (
        <p role="alert" className="text-sm text-destructive">
          {t('parent.children.startFailed')}
        </p>
      )}
    </div>
  )
}
