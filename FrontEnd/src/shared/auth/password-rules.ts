/**
 * The password rule for Admin and Parent users, as in the backend PasswordRules (owner decision 2026-10-23): 12 to
 * 128 characters with an upper-case letter, a lower-case letter, a digit and a symbol. The Unicode classes match .NET's
 * char.IsUpper, char.IsLower, char.IsDigit and "not a letter or digit".
 */
export const passwordMaxLength = 128

export const passwordRules = {
  length: (password: string) => password.length >= 12,
  upper: (password: string) => /\p{Lu}/u.test(password),
  lower: (password: string) => /\p{Ll}/u.test(password),
  digit: (password: string) => /\p{Nd}/u.test(password),
  symbol: (password: string) => /[^\p{L}\p{Nd}]/u.test(password),
} as const

export type PasswordRule = keyof typeof passwordRules

export function isStrongPassword(password: string) {
  return Object.values(passwordRules).every((rule) => rule(password))
}
