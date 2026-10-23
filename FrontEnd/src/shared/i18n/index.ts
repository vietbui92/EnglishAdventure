import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from '@/shared/i18n/locales/en.json'
import vi from '@/shared/i18n/locales/vi.json'

export const languages = ['vi', 'en'] as const
export type Language = (typeof languages)[number]

type Texts = Record<string, unknown>

// Each area keeps its texts next to its code in src/areas/<area>/i18n/{vi,en}.json, under one top-level key named
// after the area. They are collected by file name, so shared code never imports an area.
const areaTexts = import.meta.glob<Texts>('/src/areas/*/i18n/*.json', { eager: true, import: 'default' })

/** The shared texts plus every area's texts for one language. */
export function textsFor(language: Language, base: Texts): Texts {
  const merged = { ...base }
  for (const [file, texts] of Object.entries(areaTexts)) {
    if (file.endsWith(`/i18n/${language}.json`)) {
      Object.assign(merged, texts)
    }
  }
  return merged
}

export const resources = {
  vi: { translation: textsFor('vi', vi) },
  en: { translation: textsFor('en', en) },
}

/** For tests: the per-area files, keyed by path. */
export { areaTexts }

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
