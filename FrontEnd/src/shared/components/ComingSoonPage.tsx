import { useTranslation } from 'react-i18next'

export function ComingSoonPage({ title }: { title: string }) {
  const { t } = useTranslation()

  return (
    <section>
      <h1 className="font-heading text-2xl font-semibold">{title}</h1>
      <p className="mt-2 text-muted-foreground">
        {t('common.comingSoon')}. {t('common.comingSoonDescription')}
      </p>
    </section>
  )
}
