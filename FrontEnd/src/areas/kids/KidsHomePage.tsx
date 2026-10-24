import { useTranslation } from 'react-i18next'
import { useSession } from '@/shared/auth/session'
import { ChildAvatar } from '@/shared/components/ChildAvatar'
import { Button } from '@/shared/components/ui/button'

// The child's own space. The lesson map replaces the placeholder in F4.2.
export function KidsHomePage() {
  const { t } = useTranslation()
  const child = useSession()?.user.childProfile

  return (
    <div className="flex flex-col items-center gap-6 text-center">
      {child && <ChildAvatar avatarKey={child.avatarKey} size="lg" />}
      <h1 className="font-heading text-4xl font-bold">
        {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
      </h1>
      <p className="text-xl">{t('kids.title')}</p>
      <p className="text-muted-foreground">{t('kids.subtitle')}</p>
      <Button className="min-h-14 rounded-2xl px-10 text-xl" disabled>
        {t('kids.start')}
      </Button>
    </div>
  )
}
