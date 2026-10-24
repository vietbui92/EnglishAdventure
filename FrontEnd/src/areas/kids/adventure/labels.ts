import type { TFunction } from 'i18next'

/**
 * "Level n · title" for a level's sign, or just "Level n" when the Admin named the level that already (owner
 * 2026-10-24: "Level 1 · Level 1" repeated itself).
 */
export function levelLabel(t: TFunction, number: number, title: string) {
  const plain = /^\s*level\s*\d+\s*$/i.test(title)
  return plain ? t('kids.map.level', { number }) : t('kids.map.levelTitle', { number, title })
}
