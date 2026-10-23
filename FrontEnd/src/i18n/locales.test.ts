import { describe, expect, it } from 'vitest'
import en from '@/i18n/locales/en.json'
import vi from '@/i18n/locales/vi.json'

function keysOf(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) {
    return [prefix]
  }
  return Object.entries(value).flatMap(([key, child]) => keysOf(child, prefix ? `${prefix}.${key}` : key))
}

describe('locales', () => {
  it('have the same keys in Vietnamese and English', () => {
    expect(keysOf(en).sort()).toEqual(keysOf(vi).sort())
  })

  it('have no empty texts', () => {
    for (const locale of [vi, en]) {
      const empty = keysOf(locale).filter((key) => {
        const text = key.split('.').reduce<unknown>((node, part) => (node as Record<string, unknown>)[part], locale)
        return typeof text !== 'string' || text.trim() === ''
      })
      expect(empty).toEqual([])
    }
  })
})
