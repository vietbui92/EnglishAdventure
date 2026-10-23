import { describe, expect, it } from 'vitest'
import { areaTexts } from '@/shared/i18n'
import sharedEn from '@/shared/i18n/locales/en.json'
import sharedVi from '@/shared/i18n/locales/vi.json'

function keysOf(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) {
    return [prefix]
  }
  return Object.entries(value).flatMap(([key, child]) => keysOf(child, prefix ? `${prefix}.${key}` : key))
}

function textAt(texts: unknown, key: string) {
  return key.split('.').reduce<unknown>((node, part) => (node as Record<string, unknown>)[part], texts)
}

// Every translation file pair: the shared one and one per area.
const pairs: [string, unknown, unknown][] = [
  ['shared', sharedVi, sharedEn],
  ...Object.keys(areaTexts)
    .filter((file) => file.endsWith('/vi.json'))
    .map((file): [string, unknown, unknown] => [
      file,
      areaTexts[file],
      areaTexts[file.replace(/vi\.json$/, 'en.json')],
    ]),
]

describe('translations', () => {
  it('include every area', () => {
    expect(pairs.map(([name]) => name.split('/')[3] ?? name).sort()).toEqual([
      'admin',
      'kids',
      'parent',
      'public',
      'shared',
    ])
  })

  it.each(pairs)('%s has the same keys in Vietnamese and English', (_, vi, en) => {
    expect(en).toBeDefined()
    expect(keysOf(en).sort()).toEqual(keysOf(vi).sort())
  })

  it.each(pairs)('%s has no empty texts', (_, vi, en) => {
    for (const texts of [vi, en]) {
      const empty = keysOf(texts).filter((key) => {
        const text = textAt(texts, key)
        return typeof text !== 'string' || text.trim() === ''
      })
      expect(empty).toEqual([])
    }
  })

  it('give every file its own top-level keys, so merging never overwrites', () => {
    const owners = new Map<string, string>()
    for (const [name, vi] of pairs) {
      for (const key of Object.keys(vi as object)) {
        expect(owners.get(key), `"${key}" is in both ${owners.get(key)} and ${name}`).toBeUndefined()
        owners.set(key, name)
      }
    }
  })
})
