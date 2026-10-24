import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import type { BossInput } from '@/areas/admin/content/api'
import { ContentError } from '@/areas/admin/content/ContentParts'
import { isHttpsUrl } from '@/areas/admin/content/WordDialog'
import { WordPicture } from '@/areas/admin/content/WordPicture'
import { ApiError } from '@/shared/api/problem-details'
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

// Same limits as the backend ContentTitle and ImageUrl value objects.
const titleMaxLength = 100
const imageUrlMaxLength = 2000

function bossSchema(pictureRequired: boolean) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, 'admin.content.boss.form.validation.nameRequired' satisfies ParseKeys)
      .max(titleMaxLength, 'admin.content.boss.form.validation.nameTooLong' satisfies ParseKeys),
    imageUrl: z
      .string()
      .trim()
      .refine((value) => value === '' || isHttpsUrl(value), {
        message: 'admin.content.words.form.validation.imageUrlHttps' satisfies ParseKeys,
      })
      .refine((value) => !pictureRequired || value !== '', {
        message: 'admin.content.boss.form.validation.pictureRequired' satisfies ParseKeys,
      }),
  })
}

interface BossDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The boss to change; without it the dialog creates the level's boss. */
  initial?: BossInput
  /** A published boss keeps its picture. */
  pictureRequired: boolean
  /** Saves the values; a rejection keeps the dialog open and shows the error. */
  onSave: (values: BossInput) => Promise<unknown>
}

/** Create or change the level's boss (S3.8): its name and its picture. */
export function BossDialog(props: BossDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false}>
        {/* Re-created on every opening, so the form starts from the boss's current values. */}
        {props.open && <BossForm {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function BossForm({ onOpenChange, initial, pictureRequired, onSave }: BossDialogProps) {
  const { t } = useTranslation()
  const [error, setError] = useState<unknown>(null)
  const schema = bossSchema(pictureRequired)
  const form = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: { title: initial?.title ?? '', imageUrl: initial?.imageUrl ?? '' },
  })
  const { errors, isSubmitting } = form.formState
  const imageUrl = useWatch({ control: form.control, name: 'imageUrl' }).trim()

  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try {
      await onSave(values)
      onOpenChange(false)
    } catch (cause) {
      if (cause instanceof ApiError && cause.kind === 'validation') {
        form.setError(cause.fieldErrors.imageUrl ? 'imageUrl' : 'title', {
          type: 'server',
          message: cause.fieldErrors.imageUrl
            ? 'admin.content.words.form.validation.imageUrlHttps'
            : 'admin.content.boss.form.validation.nameRequired',
        })
      } else {
        setError(cause)
      }
    }
  })

  const message = (key: string | undefined) =>
    key ? <p className="text-sm text-destructive">{t(key as ParseKeys)}</p> : null

  return (
    <form noValidate onSubmit={(event) => void submit(event)} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>
          {initial ? t('admin.content.boss.form.editTitle') : t('admin.content.boss.form.addTitle')}
        </DialogTitle>
        <DialogDescription>{t('admin.content.boss.form.hint')}</DialogDescription>
      </DialogHeader>

      {error !== null && <ContentError error={error} />}

      <div className="flex flex-col gap-2">
        <Label htmlFor="boss-title">{t('admin.content.boss.form.name')}</Label>
        <Input
          id="boss-title"
          autoComplete="off"
          maxLength={titleMaxLength}
          placeholder={t('admin.content.boss.form.namePlaceholder')}
          aria-invalid={errors.title ? true : undefined}
          {...form.register('title')}
        />
        {message(errors.title?.message)}
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="boss-imageUrl">{t('admin.content.boss.form.picture')}</Label>
        <div className="flex items-start gap-3">
          <Input
            id="boss-imageUrl"
            type="url"
            inputMode="url"
            autoComplete="off"
            placeholder="https://"
            maxLength={imageUrlMaxLength}
            aria-invalid={errors.imageUrl ? true : undefined}
            {...form.register('imageUrl')}
          />
          <WordPicture url={isHttpsUrl(imageUrl) ? imageUrl : null} alt={t('admin.content.words.form.preview')} />
        </div>
        {message(errors.imageUrl?.message) ?? (
          <p className="text-sm text-muted-foreground">
            {pictureRequired
              ? t('admin.content.boss.form.pictureHintRequired')
              : t('admin.content.boss.form.pictureHint')}
          </p>
        )}
      </div>

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
          {t('admin.content.actions.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting
            ? t('admin.content.form.saving')
            : initial
              ? t('admin.content.form.save')
              : t('admin.content.form.create')}
        </Button>
      </DialogFooter>
    </form>
  )
}
