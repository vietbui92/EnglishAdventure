import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ContentError } from '@/areas/admin/content/ContentParts'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { Textarea } from '@/shared/components/ui/textarea'
import { zodResolver } from '@/shared/lib/zod-resolver'

// Same limits as the backend ContentTitle value object and Level.DescriptionMaxLength.
const titleMaxLength = 100
const descriptionMaxLength = 500

const contentSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'admin.content.form.validation.titleRequired' satisfies ParseKeys)
    .max(titleMaxLength, 'admin.content.form.validation.titleTooLong' satisfies ParseKeys),
  description: z
    .string()
    .trim()
    .max(descriptionMaxLength, 'admin.content.form.validation.descriptionTooLong' satisfies ParseKeys),
})

export type ContentFormValues = z.output<typeof contentSchema>

interface ContentFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  heading: string
  submitLabel: string
  /** Levels have a description; lessons only a title. */
  withDescription: boolean
  initial?: Partial<ContentFormValues>
  /** Saves the values; a rejection keeps the dialog open and shows the error. */
  onSave: (values: ContentFormValues) => Promise<unknown>
}

/** Create or edit a level (title and description) or a lesson (title). */
export function ContentFormDialog(props: ContentFormDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false}>
        {/* Re-created on every opening, so the form starts from the item's current values. */}
        {props.open && <ContentForm {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function ContentForm({ onOpenChange, heading, submitLabel, withDescription, initial, onSave }: ContentFormDialogProps) {
  const { t } = useTranslation()
  const [error, setError] = useState<unknown>(null)
  const form = useForm<z.input<typeof contentSchema>, unknown, ContentFormValues>({
    resolver: zodResolver(contentSchema),
    defaultValues: { title: initial?.title ?? '', description: initial?.description ?? '' },
  })
  const { errors, isSubmitting } = form.formState

  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try {
      await onSave(values)
      onOpenChange(false)
    } catch (cause) {
      const invalid = cause instanceof ApiError && cause.kind === 'validation'
      setError(invalid ? null : cause)
      if (invalid) {
        // The server's messages are English; the form shows its own text for the field the server names.
        if (cause instanceof ApiError && cause.fieldErrors.description) {
          form.setError('description', { type: 'server', message: 'admin.content.form.validation.descriptionTooLong' })
        } else {
          form.setError('title', { type: 'server', message: 'admin.content.form.validation.titleRequired' })
        }
      }
    }
  })

  return (
    <form noValidate onSubmit={(event) => void submit(event)} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>{heading}</DialogTitle>
      </DialogHeader>

      {error !== null && <ContentError error={error} />}

      <div className="flex flex-col gap-2">
        <Label htmlFor="content-title">{t('admin.content.form.title')}</Label>
        <Input
          id="content-title"
          autoComplete="off"
          maxLength={titleMaxLength}
          aria-invalid={errors.title ? true : undefined}
          aria-describedby={errors.title ? 'content-title-error' : undefined}
          {...form.register('title')}
        />
        {errors.title?.message && (
          <p id="content-title-error" className="text-sm text-destructive">
            {t(errors.title.message as ParseKeys)}
          </p>
        )}
      </div>

      {withDescription && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="content-description">{t('admin.content.form.description')}</Label>
          <Textarea
            id="content-description"
            maxLength={descriptionMaxLength}
            aria-invalid={errors.description ? true : undefined}
            aria-describedby={errors.description ? 'content-description-error' : 'content-description-hint'}
            {...form.register('description')}
          />
          {errors.description?.message ? (
            <p id="content-description-error" className="text-sm text-destructive">
              {t(errors.description.message as ParseKeys)}
            </p>
          ) : (
            <p id="content-description-hint" className="text-sm text-muted-foreground">
              {t('admin.content.form.descriptionHint')}
            </p>
          )}
        </div>
      )}

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
          {t('admin.content.actions.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('admin.content.form.saving') : submitLabel}
        </Button>
      </DialogFooter>
    </form>
  )
}
