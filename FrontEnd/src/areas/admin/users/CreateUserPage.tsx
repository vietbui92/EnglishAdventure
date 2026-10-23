import type { ParseKeys } from 'i18next'
import { ArrowLeft, Check, CircleAlert, X } from 'lucide-react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { z } from 'zod'
import { ApiError } from '@/shared/api/problem-details'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/shared/components/ui/radio-group'
import { useCreateUser } from '@/areas/admin/users/api'
import {
  isStrongPassword,
  passwordMaxLength,
  passwordRules,
  type PasswordRule,
} from '@/areas/admin/users/password-rules'
import { zodResolver } from '@/shared/lib/zod-resolver'
import { cn } from '@/shared/lib/utils'

/** Tells the detail page that the user was just created, so it can explain the next step. */
export interface CreatedUserLocationState {
  created?: boolean
}

// Same rules as the backend CreateUserCommandValidator; messages are translation keys.
const createUserSchema = z
  .object({
    email: z
      .string()
      .trim()
      .min(1, 'admin.users.create.validation.emailRequired' satisfies ParseKeys)
      .max(256, 'admin.users.create.validation.emailTooLong' satisfies ParseKeys)
      .pipe(z.email('admin.users.create.validation.emailInvalid' satisfies ParseKeys)),
    userType: z.enum(['Admin', 'Parent']),
    password: z
      .string()
      .max(passwordMaxLength, 'admin.users.create.validation.passwordTooLong' satisfies ParseKeys)
      .refine(isStrongPassword, 'admin.users.create.validation.passwordWeak' satisfies ParseKeys),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'admin.users.create.validation.passwordMismatch' satisfies ParseKeys,
    path: ['confirmPassword'],
  })

type CreateUserForm = z.input<typeof createUserSchema>

const ruleLabels: Record<PasswordRule, ParseKeys> = {
  length: 'admin.users.create.passwordRules.length',
  upper: 'admin.users.create.passwordRules.upper',
  lower: 'admin.users.create.passwordRules.lower',
  digit: 'admin.users.create.passwordRules.digit',
  symbol: 'admin.users.create.passwordRules.symbol',
}

export function CreateUserPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const form = useForm<CreateUserForm, unknown, z.output<typeof createUserSchema>>({
    resolver: zodResolver(createUserSchema),
    defaultValues: { email: '', userType: 'Admin', password: '', confirmPassword: '' },
  })
  const { errors } = form.formState
  const password = useWatch({ control: form.control, name: 'password' })

  const createUser = useCreateUser()

  const submit = form.handleSubmit(({ email, userType, password: newPassword }) =>
    createUser.mutate(
      { email, userType, password: newPassword },
      {
        onSuccess: (created) =>
          navigate(`/admin/users/${created.id}`, { state: { created: true } satisfies CreatedUserLocationState }),
        onError: (error) => {
          if (error instanceof ApiError && error.kind === 'conflict') {
            form.setError('email', { type: 'server', message: 'admin.users.create.errors.duplicate' })
          } else if (error instanceof ApiError && error.kind === 'validation') {
            for (const field of ['email', 'password'] as const) {
              if (error.fieldErrors[field]) {
                form.setError(field, { type: 'server', message: 'admin.users.create.validation.serverInvalid' })
              }
            }
          }
        },
      },
    ),
  )

  const fieldError = (field: keyof CreateUserForm) => {
    const message = errors[field]?.message
    return message ? (
      <p id={`${field}-error`} className="text-sm text-destructive">
        {t(message as ParseKeys)}
      </p>
    ) : null
  }
  const describedBy = (field: keyof CreateUserForm, extra?: string) =>
    [errors[field] ? `${field}-error` : undefined, extra].filter(Boolean).join(' ') || undefined

  // Conflicts and validation errors are shown on the fields; anything else gets a general message.
  const generalError =
    createUser.isError &&
    !(createUser.error instanceof ApiError && ['conflict', 'validation'].includes(createUser.error.kind))

  return (
    <section className="flex max-w-lg flex-col gap-6">
      <Button asChild variant="ghost" size="sm" className="self-start">
        <Link to="/admin/users">
          <ArrowLeft aria-hidden />
          {t('admin.users.detail.back')}
        </Link>
      </Button>

      <div>
        <h1 className="font-heading text-2xl font-semibold">{t('admin.users.create.title')}</h1>
        <p className="mt-1 text-muted-foreground">{t('admin.users.create.description')}</p>
      </div>

      <form noValidate onSubmit={submit} className="flex flex-col gap-5">
        {generalError && (
          <Alert variant="destructive">
            <CircleAlert aria-hidden />
            <AlertDescription>{t('admin.users.create.errors.failed')}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-2">
          <Label htmlFor="email">{t('admin.users.create.email')}</Label>
          <Input
            id="email"
            type="email"
            autoComplete="off"
            aria-invalid={errors.email ? true : undefined}
            aria-describedby={describedBy('email')}
            {...form.register('email')}
          />
          {fieldError('email')}
        </div>

        <fieldset className="flex flex-col gap-3">
          <legend className="mb-2 text-sm font-medium">{t('admin.users.create.userType')}</legend>
          <Controller
            control={form.control}
            name="userType"
            render={({ field }) => (
              <RadioGroup value={field.value} onValueChange={field.onChange} className="gap-3">
                {(['Admin', 'Parent'] as const).map((userType) => (
                  <div key={userType} className="flex items-start gap-3">
                    <RadioGroupItem
                      id={`userType-${userType}`}
                      value={userType}
                      aria-describedby={`userType-${userType}-hint`}
                    />
                    <div className="grid gap-0.5">
                      <Label htmlFor={`userType-${userType}`}>{t(`admin.users.create.userType${userType}`)}</Label>
                      <span id={`userType-${userType}-hint`} className="text-sm text-muted-foreground">
                        {t(`admin.users.create.userType${userType}Hint`)}
                      </span>
                    </div>
                  </div>
                ))}
              </RadioGroup>
            )}
          />
        </fieldset>

        <div className="flex flex-col gap-2">
          <Label htmlFor="password">{t('admin.users.create.password')}</Label>
          <Input
            id="password"
            type="password"
            autoComplete="new-password"
            aria-invalid={errors.password ? true : undefined}
            aria-describedby={describedBy('password', 'password-rules')}
            {...form.register('password')}
          />
          {fieldError('password')}
          <div id="password-rules" className="text-sm">
            <p className="text-muted-foreground">{t('admin.users.create.passwordRules.title')}</p>
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
                    <span className="sr-only">
                      ({met ? t('admin.users.create.passwordRules.met') : t('admin.users.create.passwordRules.notMet')})
                    </span>
                  </li>
                )
              })}
            </ul>
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="confirmPassword">{t('admin.users.create.confirmPassword')}</Label>
          <Input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            aria-invalid={errors.confirmPassword ? true : undefined}
            aria-describedby={describedBy('confirmPassword')}
            {...form.register('confirmPassword')}
          />
          {fieldError('confirmPassword')}
        </div>

        <div className="flex gap-2">
          <Button type="submit" disabled={createUser.isPending}>
            {createUser.isPending ? t('admin.users.create.submitting') : t('admin.users.create.submit')}
          </Button>
          <Button asChild variant="ghost">
            <Link to="/admin/users">{t('admin.users.create.cancel')}</Link>
          </Button>
        </div>
      </form>
    </section>
  )
}
