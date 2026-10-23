import 'i18next'
import type admin from '@/areas/admin/i18n/vi.json'
import type kids from '@/areas/kids/i18n/vi.json'
import type parent from '@/areas/parent/i18n/vi.json'
import type publicArea from '@/areas/public/i18n/vi.json'
import type shared from '@/shared/i18n/locales/vi.json'

// Type-checks every t('...') key against the Vietnamese source files. Add a new area's vi.json here.
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation'
    resources: { translation: typeof shared & typeof publicArea & typeof admin & typeof parent & typeof kids }
  }
}
