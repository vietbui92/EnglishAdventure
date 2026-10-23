import type { ParseKeys } from 'i18next'
import { Check, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { passwordRules, type PasswordRule } from '@/shared/auth/password-rules'
import { cn } from '@/shared/lib/utils'

const ruleLabels: Record<PasswordRule, ParseKeys> = {
  length: 'password.rules.length',
  upper: 'password.rules.upper',
  lower: 'password.rules.lower',
  digit: 'password.rules.digit',
  symbol: 'password.rules.symbol',
}

/**
 * The password rule as a live checklist under a new-password field. Point the field's aria-describedby at `id`.
 */
export function PasswordChecklist({ id, password }: { id: string; password: string }) {
  const { t } = useTranslation()

  return (
    <div id={id} className="text-sm">
      <p className="text-muted-foreground">{t('password.title')}</p>
      <ul className="mt-1 grid gap-0.5">
        {(Object.keys(passwordRules) as PasswordRule[]).map((rule) => {
          const met = passwordRules[rule](password)
          return (
            <li
              key={rule}
              className={cn('flex items-center gap-1.5', met ? 'text-foreground' : 'text-muted-foreground')}
            >
              {met ? <Check className="size-4 text-primary" aria-hidden /> : <X className="size-4" aria-hidden />}
              {t(ruleLabels[rule])}
              <span className="sr-only">({met ? t('password.met') : t('password.notMet')})</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
