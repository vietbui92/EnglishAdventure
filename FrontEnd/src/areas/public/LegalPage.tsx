import { useTranslation } from 'react-i18next'
import { PublicHeader } from '@/areas/public/PublicHeader'

/**
 * Terms of use and privacy policy. Provisional: the final text depends on OQ-008 (markets, consent, retention).
 * When it changes, raise Consent.CurrentVersion in the backend so each Parent's record shows what they accepted.
 */
export function LegalPage({ document }: { document: 'terms' | 'privacy' }) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-svh flex-col">
      <PublicHeader />
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-12">
        <h1 className="font-heading text-3xl font-bold">{t(`public.legal.${document}`)}</h1>
        <p className="mt-4 text-muted-foreground">{t('public.legal.draft')}</p>
      </main>
    </div>
  )
}

export function TermsPage() {
  return <LegalPage document="terms" />
}

export function PrivacyPage() {
  return <LegalPage document="privacy" />
}
