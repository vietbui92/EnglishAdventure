import { useMutation } from '@tanstack/react-query'
import type { ParseKeys } from 'i18next'
import { CircleAlert, Clock } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { ApiError } from '@/shared/api/problem-details'
import { RestoringSession, type LoginLocationState } from '@/shared/auth/guards'
import { useSessionState } from '@/shared/auth/session'
import { pathAfterSignIn, signIn } from '@/shared/auth/sign-in'
import { LanguageSwitcher } from '@/shared/components/LanguageSwitcher'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { Button } from '@/shared/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { zodResolver } from '@/shared/lib/zod-resolver'

// Messages are translation keys, so they follow a language switch. Same limits as the backend LoginCommandValidator.
const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'public.login.validation.emailRequired' satisfies ParseKeys)
    .max(256, 'public.login.validation.emailTooLong' satisfies ParseKeys)
    .pipe(z.email('public.login.validation.emailInvalid' satisfies ParseKeys)),
  password: z
    .string()
    .min(1, 'public.login.validation.passwordRequired' satisfies ParseKeys)
    .max(256, 'public.login.validation.passwordTooLong' satisfies ParseKeys),
})

type LoginForm = z.input<typeof loginSchema>

function formErrorKey(error: unknown): ParseKeys {
  if (!(error instanceof ApiError)) {
    return 'public.login.errors.unavailable'
  }
  switch (error.kind) {
    case 'unauthorized':
      return 'public.login.errors.invalidCredentials'
    case 'forbidden':
      return 'public.login.errors.inactive'
    case 'validation':
      return 'public.login.errors.invalidInput'
    default:
      return 'public.login.errors.unavailable'
  }
}

export function LoginPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as LoginLocationState | null)?.from
  const sessionState = useSessionState()
  const form = useForm<LoginForm, unknown, z.output<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })
  const { errors } = form.formState

  const login = useMutation({
    mutationFn: signIn,
    onSuccess: (signedIn) => navigate(pathAfterSignIn(signedIn.user, from), { replace: true }),
    onError: (error) => {
      // The client rules mirror the server's, so this only happens if they drift apart.
      if (error instanceof ApiError && error.kind === 'validation') {
        for (const field of ['email', 'password'] as const) {
          if (error.fieldErrors[field]) {
            form.setError(field, { type: 'server', message: 'public.login.validation.serverInvalid' })
          }
        }
      }
    },
  })

  if (sessionState.status === 'restoring') {
    return <RestoringSession />
  }
  if (sessionState.status === 'signed-in' && !login.isPending) {
    return <Navigate to={pathAfterSignIn(sessionState.session.user, from)} replace />
  }
  const expired = sessionState.status === 'signed-out' && sessionState.reason === 'expired'

  const fieldError = (field: keyof LoginForm) => {
    const message = errors[field]?.message
    return message ? (
      <p id={`${field}-error`} className="text-sm text-destructive">
        {t(message as ParseKeys)}
      </p>
    ) : null
  }

  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-6 bg-muted/40 p-4">
      <div className="flex w-full max-w-sm items-center justify-between">
        <Link to="/" className="font-heading font-semibold">
          {t('app.name')}
        </Link>
        <LanguageSwitcher />
      </div>

      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>
            <h1 className="font-heading text-xl font-semibold">{t('public.login.title')}</h1>
          </CardTitle>
          <CardDescription>{t('public.login.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            noValidate
            className="flex flex-col gap-4"
            onSubmit={form.handleSubmit((values) => login.mutate(values))}
          >
            {expired && !login.isError && (
              <Alert>
                <Clock aria-hidden />
                <AlertDescription>{t('public.login.expired')}</AlertDescription>
              </Alert>
            )}
            {login.isError && (
              <Alert variant="destructive">
                <CircleAlert aria-hidden />
                <AlertDescription>{t(formErrorKey(login.error))}</AlertDescription>
              </Alert>
            )}

            <div className="flex flex-col gap-2">
              <Label htmlFor="email">{t('public.login.email')}</Label>
              <Input
                id="email"
                type="email"
                autoComplete="username"
                autoFocus
                aria-invalid={errors.email ? true : undefined}
                aria-describedby={errors.email ? 'email-error' : undefined}
                {...form.register('email')}
              />
              {fieldError('email')}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="password">{t('public.login.password')}</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                aria-invalid={errors.password ? true : undefined}
                aria-describedby={errors.password ? 'password-error' : undefined}
                {...form.register('password')}
              />
              {fieldError('password')}
            </div>

            <Button type="submit" className="mt-2" disabled={login.isPending}>
              {login.isPending ? t('public.login.submitting') : t('public.login.submit')}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  )
}
