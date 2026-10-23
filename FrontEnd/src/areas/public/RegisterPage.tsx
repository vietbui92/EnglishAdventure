import type { ParseKeys } from 'i18next'
import { CircleAlert } from 'lucide-react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { Trans, useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { z } from 'zod'
import { useRegister } from '@/areas/public/api'
import { PublicHeader } from '@/areas/public/PublicHeader'
import { ApiError } from '@/shared/api/problem-details'
import { isStrongPassword, passwordMaxLength } from '@/shared/auth/password-rules'
import { PasswordChecklist } from '@/shared/components/PasswordChecklist'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { Button } from '@/shared/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'
import { Checkbox } from '@/shared/components/ui/checkbox'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { zodResolver } from '@/shared/lib/zod-resolver'

/** Tells the "check your email" page which address the link went to. */
export interface RegisteredLocationState {
  email?: string
}

// Same rules as the backend RegisterParentCommandValidator; messages are translation keys.
const registerSchema = z
  .object({
    email: z
      .string()
      .trim()
      .min(1, 'public.register.validation.emailRequired' satisfies ParseKeys)
      .max(256, 'public.register.validation.emailTooLong' satisfies ParseKeys)
      .pipe(z.email('public.register.validation.emailInvalid' satisfies ParseKeys)),
    password: z
      .string()
      .max(passwordMaxLength, 'public.register.validation.passwordTooLong' satisfies ParseKeys)
      .refine(isStrongPassword, 'public.register.validation.passwordWeak' satisfies ParseKeys),
    confirmPassword: z.string(),
    acceptTerms: z
      .boolean()
      .refine((accepted) => accepted, 'public.register.validation.termsRequired' satisfies ParseKeys),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'public.register.validation.passwordMismatch' satisfies ParseKeys,
    path: ['confirmPassword'],
  })

type RegisterForm = z.input<typeof registerSchema>

/** Parent self-registration (PD-009). On success the API has emailed a confirmation link. */
export function RegisterPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const form = useForm<RegisterForm, unknown, z.output<typeof registerSchema>>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', password: '', confirmPassword: '', acceptTerms: false },
  })
  const { errors } = form.formState
  const password = useWatch({ control: form.control, name: 'password' })
  const register = useRegister()

  const submit = form.handleSubmit(({ email, password: newPassword }) =>
    register.mutate(
      { email, password: newPassword },
      {
        onSuccess: () =>
          navigate('/register/check-email', { state: { email } satisfies RegisteredLocationState, replace: true }),
        onError: (error) => {
          if (error instanceof ApiError && error.kind === 'validation') {
            for (const field of ['email', 'password'] as const) {
              if (error.fieldErrors[field]) {
                form.setError(field, { type: 'server', message: 'public.register.validation.serverInvalid' })
              }
            }
          }
        },
      },
    ),
  )

  const fieldError = (field: keyof RegisterForm) => {
    const message = errors[field]?.message
    return message ? (
      <p id={`${field}-error`} className="text-sm text-destructive">
        {t(message as ParseKeys)}
      </p>
    ) : null
  }
  const describedBy = (field: keyof RegisterForm, extra?: string) =>
    [errors[field] ? `${field}-error` : undefined, extra].filter(Boolean).join(' ') || undefined

  const generalError = register.isError && !(register.error instanceof ApiError && register.error.kind === 'validation')

  return (
    <div className="flex min-h-svh flex-col bg-muted/40">
      <PublicHeader />
      <main className="flex flex-1 items-start justify-center p-4 sm:items-center">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>
              <h1 className="font-heading text-xl font-semibold">{t('public.register.title')}</h1>
            </CardTitle>
            <CardDescription>{t('public.register.description')}</CardDescription>
          </CardHeader>
          <CardContent>
            <form noValidate onSubmit={submit} className="flex flex-col gap-4">
              {generalError && (
                <Alert variant="destructive">
                  <CircleAlert aria-hidden />
                  <AlertDescription>
                    {register.error instanceof ApiError && register.error.kind === 'rate-limited'
                      ? t('public.tooManyAttempts')
                      : t('public.register.errors.failed')}
                  </AlertDescription>
                </Alert>
              )}

              <div className="flex flex-col gap-2">
                <Label htmlFor="email">{t('public.register.email')}</Label>
                <Input
                  id="email"
                  type="email"
                  autoComplete="email"
                  aria-invalid={errors.email ? true : undefined}
                  aria-describedby={describedBy('email')}
                  {...form.register('email')}
                />
                {fieldError('email')}
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="password">{t('public.register.password')}</Label>
                <Input
                  id="password"
                  type="password"
                  autoComplete="new-password"
                  aria-invalid={errors.password ? true : undefined}
                  aria-describedby={describedBy('password', 'password-rules')}
                  {...form.register('password')}
                />
                {fieldError('password')}
                <PasswordChecklist id="password-rules" password={password} />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="confirmPassword">{t('public.register.confirmPassword')}</Label>
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

              <div className="flex flex-col gap-2">
                <div className="flex items-start gap-3">
                  <Controller
                    control={form.control}
                    name="acceptTerms"
                    render={({ field }) => (
                      <Checkbox
                        id="acceptTerms"
                        checked={field.value}
                        onCheckedChange={(checked) => field.onChange(checked === true)}
                        aria-invalid={errors.acceptTerms ? true : undefined}
                        aria-describedby={describedBy('acceptTerms')}
                      />
                    )}
                  />
                  <Label htmlFor="acceptTerms" className="leading-snug font-normal">
                    <Trans
                      i18nKey="public.register.acceptTerms"
                      components={{
                        terms: <Link to="/terms" target="_blank" className="underline underline-offset-4" />,
                        privacy: <Link to="/privacy" target="_blank" className="underline underline-offset-4" />,
                      }}
                    />
                  </Label>
                </div>
                {fieldError('acceptTerms')}
              </div>

              <Button type="submit" disabled={register.isPending}>
                {register.isPending ? t('public.register.submitting') : t('public.register.submit')}
              </Button>

              <p className="text-sm text-muted-foreground">
                {t('public.register.haveAccount')}{' '}
                <Link to="/login" className="font-medium text-foreground underline underline-offset-4">
                  {t('public.register.signIn')}
                </Link>
              </p>
            </form>
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
