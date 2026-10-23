import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/components/ui/button'
import { languages } from '@/shared/i18n'

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()

  return (
    <div role="group" aria-label={t('common.language')} className="flex gap-1">
      {languages.map((language) => (
        <Button
          key={language}
          size="sm"
          variant={i18n.resolvedLanguage === language ? 'secondary' : 'ghost'}
          aria-pressed={i18n.resolvedLanguage === language}
          onClick={() => void i18n.changeLanguage(language)}
        >
          {t(`language.${language}`)}
        </Button>
      ))}
    </div>
  )
}
