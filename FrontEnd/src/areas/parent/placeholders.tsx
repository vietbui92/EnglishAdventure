import { useTranslation } from 'react-i18next'
import { ComingSoonPage } from '@/shared/components/ComingSoonPage'

// Parent pages that later steps replace: progress (F3.3).

export function ParentProgressPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('parent.nav.progress')} />
}
