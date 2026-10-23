import type { ParseKeys } from 'i18next'
import { CircleAlert } from 'lucide-react'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import {
  useCreateChildProfile,
  useUpdateChildProfile,
  type ChildProfile,
  type ChildProfileInput,
} from '@/areas/parent/children/api'
import { ApiError } from '@/shared/api/problem-details'
import { ChildAvatar } from '@/shared/components/ChildAvatar'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { zodResolver } from '@/shared/lib/zod-resolver'
import { cn } from '@/shared/lib/utils'

// Same rules as the backend Nickname value object; messages are translation keys.
const profileSchema = z.object({
  nickname: z
    .string()
    .trim()
    .min(1, 'parent.children.form.validation.nicknameRequired' satisfies ParseKeys)
    .max(50, 'parent.children.form.validation.nicknameTooLong' satisfies ParseKeys),
  avatarKey: z.string().min(1),
})

type ProfileForm = z.input<typeof profileSchema>

interface ChildProfileDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  avatarKeys: string[]
  maxActive: number
  /** The profile to edit; without it the dialog creates a new one. */
  profile?: ChildProfile
}

/** Create or edit a child profile: a nickname and one of the drawn avatars. */
export function ChildProfileDialog(props: ChildProfileDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false}>
        {/* Re-created on every opening, so the form starts from the profile's current values. */}
        {props.open && <ProfileForm {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function ProfileForm({ onOpenChange, avatarKeys, maxActive, profile }: ChildProfileDialogProps) {
  const { t } = useTranslation()
  const create = useCreateChildProfile()
  const update = useUpdateChildProfile(profile?.id ?? '')
  const save = profile ? update : create

  const form = useForm<ProfileForm, unknown, z.output<typeof profileSchema>>({
    resolver: zodResolver(profileSchema),
    defaultValues: { nickname: profile?.nickname ?? '', avatarKey: profile?.avatarKey ?? avatarKeys[0] ?? '' },
  })
  const { errors } = form.formState

  const submit = form.handleSubmit((values: ChildProfileInput) =>
    save.mutate(values, {
      onSuccess: () => onOpenChange(false),
      onError: (error) => {
        if (error instanceof ApiError && error.kind === 'conflict') {
          form.setError('nickname', { type: 'server', message: 'parent.children.form.validation.nicknameTaken' })
        }
      },
    }),
  )

  const generalError =
    save.isError && !(save.error instanceof ApiError && save.error.kind === 'conflict')
      ? save.error instanceof ApiError && save.error.kind === 'rule'
        ? t('parent.children.form.errors.limit', { max: maxActive })
        : t('parent.children.form.errors.failed')
      : null

  return (
    <form noValidate onSubmit={submit} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>
          {profile ? t('parent.children.form.editTitle') : t('parent.children.form.createTitle')}
        </DialogTitle>
        <DialogDescription>{t('parent.children.form.nicknameHint')}</DialogDescription>
      </DialogHeader>

      {generalError && (
        <Alert variant="destructive">
          <CircleAlert aria-hidden />
          <AlertDescription>{generalError}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="nickname">{t('parent.children.form.nickname')}</Label>
        <Input
          id="nickname"
          autoComplete="off"
          maxLength={50}
          aria-invalid={errors.nickname ? true : undefined}
          aria-describedby={errors.nickname ? 'nickname-error' : undefined}
          {...form.register('nickname')}
        />
        {errors.nickname?.message && (
          <p id="nickname-error" className="text-sm text-destructive">
            {t(errors.nickname.message as ParseKeys)}
          </p>
        )}
      </div>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium">{t('parent.children.form.avatar')}</legend>
        <Controller
          control={form.control}
          name="avatarKey"
          render={({ field }) => (
            <div
              role="radiogroup"
              aria-label={t('parent.children.form.avatar')}
              className="grid grid-cols-4 gap-2 sm:grid-cols-6"
            >
              {avatarKeys.map((key) => {
                const selected = field.value === key
                return (
                  <button
                    key={key}
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    onClick={() => field.onChange(key)}
                    className={cn(
                      'flex items-center justify-center rounded-xl border-2 p-1 transition-colors',
                      selected ? 'border-primary bg-primary/5' : 'border-transparent hover:bg-muted',
                    )}
                  >
                    <ChildAvatar avatarKey={key} size="sm" labelled />
                  </button>
                )
              })}
            </div>
          )}
        />
      </fieldset>

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
          {t('parent.children.form.cancel')}
        </Button>
        <Button type="submit" disabled={save.isPending}>
          {save.isPending
            ? t('parent.children.form.saving')
            : profile
              ? t('parent.children.form.save')
              : t('parent.children.form.create')}
        </Button>
      </DialogFooter>
    </form>
  )
}
