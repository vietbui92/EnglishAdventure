import 'i18next'
import type vi from '@/i18n/locales/vi.json'

// Type-checks every t('...') key against the Vietnamese source file.
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation'
    resources: { translation: typeof vi }
  }
}
