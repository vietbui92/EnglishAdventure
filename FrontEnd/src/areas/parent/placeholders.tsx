import { useTranslation } from 'react-i18next'
import { ComingSoonPage } from '@/shared/components/ComingSoonPage'

// Parent pages that later steps replace: child profiles (F3.1) and progress (F3.3).

export function ParentChildrenPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('parent.nav.children')} />
}

export function ParentProgressPage() {
  const { t } = useTranslation()
  return <ComingSoonPage title={t('parent.nav.progress')} />
}
