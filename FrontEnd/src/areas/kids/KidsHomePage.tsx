import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/components/ui/button'

// Replaced by the lesson map in F4.2.
export function KidsHomePage() {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col items-center gap-6 text-center">
      <h1 className="font-heading text-4xl font-bold">{t('kids.title')}</h1>
      <p className="text-muted-foreground">{t('kids.subtitle')}</p>
      <Button className="min-h-14 rounded-2xl px-10 text-xl" disabled>
        {t('kids.start')}
      </Button>
    </div>
  )
}
