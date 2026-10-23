import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from '@/i18n/locales/en.json'
import vi from '@/i18n/locales/vi.json'

export const languages = ['vi', 'en'] as const
export type Language = (typeof languages)[number]

export const resources = {
  vi: { translation: vi },
  en: { translation: en },
} as const

// Only a per-browser convenience; the app works the same when storage is unavailable.
const storageKey = 'ea.language'

function readStoredLanguage(): Language | undefined {
  try {
    const stored = localStorage.getItem(storageKey)
    return languages.find((language) => language === stored)
  } catch {
    return undefined
  }
}

i18n.on('languageChanged', (language) => {
  document.documentElement.lang = language
  try {
    localStorage.setItem(storageKey, language)
  } catch {
    // Storage blocked: the choice lasts for this page load only.
  }
})

void i18n.use(initReactI18next).init({
  resources,
  lng: readStoredLanguage() ?? 'vi',
  // Vietnamese is the source language; English fills any gap.
  fallbackLng: 'en',
  supportedLngs: languages,
  interpolation: { escapeValue: false },
  initAsync: false,
})

export default i18n
