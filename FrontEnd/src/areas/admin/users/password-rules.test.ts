import { describe, expect, it } from 'vitest'
import { isStrongPassword, passwordRules } from '@/areas/admin/users/password-rules'

describe('password rules', () => {
  it('accepts a password that meets every rule', () => {
    expect(isStrongPassword('Correct-horse-1')).toBe(true)
  })

  it.each([
    ['Short-pw-1', 'length'],
    ['no-upper-case-1', 'upper'],
    ['NO-LOWER-CASE-1', 'lower'],
    ['No-digits-at-all', 'digit'],
    ['NoSymbolsHere123', 'symbol'],
  ] as const)('rejects %s for the %s rule', (password, rule) => {
    expect(passwordRules[rule](password)).toBe(false)
    expect(isStrongPassword(password)).toBe(false)
  })

  it('counts letters and digits beyond ASCII like the backend', () => {
    // Vietnamese letters are letters, not symbols; a space is a symbol.
    expect(passwordRules.upper('Đ')).toBe(true)
    expect(passwordRules.lower('ư')).toBe(true)
    expect(passwordRules.symbol('Đường')).toBe(false)
    expect(passwordRules.symbol('a b')).toBe(true)
  })
})
