import { useTranslation } from 'react-i18next'

/** Formats an ISO date-time from the API in the interface language, e.g. "23 thg 10, 2026, 14:05". */
export function useDateTimeFormat() {
  const { i18n } = useTranslation()
  const formatter = new Intl.DateTimeFormat(i18n.resolvedLanguage, { dateStyle: 'medium', timeStyle: 'short' })
  return (value: string) => formatter.format(new Date(value))
}
