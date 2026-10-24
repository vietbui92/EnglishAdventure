import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import type { LessonWord, WordInput } from '@/areas/admin/content/api'
import { ContentError } from '@/areas/admin/content/ContentParts'
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

// Same limits as the backend LessonWord entity and ImageUrl value object (S3.6).
const englishMaxLength = 50
const meaningMaxLength = 100
const imageUrlMaxLength = 2000

export function isHttpsUrl(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'https:' && url.hostname.length > 0 && value.length <= imageUrlMaxLength
  } catch {
    return false
  }
}

function wordSchema(pictureRequired: boolean) {
  return z.object({
    english: z
      .string()
      .trim()
      .min(1, 'admin.content.words.form.validation.englishRequired' satisfies ParseKeys)
      .max(englishMaxLength, 'admin.content.words.form.validation.englishTooLong' satisfies ParseKeys),
    meaning: z
      .string()
      .trim()
      .min(1, 'admin.content.words.form.validation.meaningRequired' satisfies ParseKeys)
      .max(meaningMaxLength, 'admin.content.words.form.validation.meaningTooLong' satisfies ParseKeys),
    imageUrl: z
      .string()
      .trim()
      .refine((value) => value === '' || isHttpsUrl(value), {
        message: 'admin.content.words.form.validation.imageUrlHttps' satisfies ParseKeys,
      })
      .refine((value) => !pictureRequired || value !== '', {
        message: 'admin.content.words.form.validation.imageUrlRequired' satisfies ParseKeys,
      }),
  })
}

interface WordDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The word to correct; without it the dialog adds a new one. */
  word?: LessonWord
  /** A published lesson's words must keep their pictures. */
  pictureRequired: boolean
  /** Saves the values; a rejection keeps the dialog open and shows the error. */
  onSave: (values: WordInput) => Promise<unknown>
}

/** Add or correct a word: the English word, its Vietnamese meaning and a picture address. */
export function WordDialog(props: WordDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false}>
        {/* Re-created on every opening, so the form starts from the word's current values. */}
        {props.open && <WordForm {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function WordForm({ onOpenChange, word, pictureRequired, onSave }: WordDialogProps) {
  const { t } = useTranslation()
  const [error, setError] = useState<unknown>(null)
  const schema = wordSchema(pictureRequired)
  const form = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: { english: word?.english ?? '', meaning: word?.meaning ?? '', imageUrl: word?.imageUrl ?? '' },
  })
  const { errors, isSubmitting } = form.formState
  const imageUrl = form.watch('imageUrl').trim()

  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try {
      await onSave(values)
      onOpenChange(false)
    } catch (cause) {
      if (cause instanceof ApiError && cause.code === 'word-duplicate') {
        form.setError('english', { type: 'server', message: 'admin.content.words.form.validation.duplicate' })
      } else if (cause instanceof ApiError && cause.kind === 'validation') {
        // The server's messages are English; show the form's own text on the field the server names.
        const field = (['imageUrl', 'meaning', 'english'] as const).find((name) => cause.fieldErrors[name]) ?? 'english'
        form.setError(field, { type: 'server', message: serverFieldKeys[field] })
      } else {
        setError(cause)
      }
    }
  })

  const fieldError = (name: 'english' | 'meaning' | 'imageUrl') => {
    const message = errors[name]?.message
    return message ? (
      <p id={`word-${name}-error`} className="text-sm text-destructive">
        {t(message as ParseKeys)}
      </p>
    ) : null
  }

  return (
    <form noValidate onSubmit={(event) => void submit(event)} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>
          {word ? t('admin.content.words.form.editTitle') : t('admin.content.words.form.addTitle')}
        </DialogTitle>
        <DialogDescription>{t('admin.content.words.form.hint')}</DialogDescription>
      </DialogHeader>

      {error !== null && <ContentError error={error} />}

      <div className="flex flex-col gap-2">
        <Label htmlFor="word-english">{t('admin.content.words.form.english')}</Label>
        <Input
          id="word-english"
          lang="en"
          autoComplete="off"
          maxLength={englishMaxLength}
          aria-invalid={errors.english ? true : undefined}
          aria-describedby={errors.english ? 'word-english-error' : undefined}
          {...form.register('english')}
        />
        {fieldError('english')}
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="word-meaning">{t('admin.content.words.form.meaning')}</Label>
        <Input
          id="word-meaning"
          autoComplete="off"
          maxLength={meaningMaxLength}
          aria-invalid={errors.meaning ? true : undefined}
          aria-describedby={errors.meaning ? 'word-meaning-error' : undefined}
          {...form.register('meaning')}
        />
        {fieldError('meaning')}
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="word-imageUrl">{t('admin.content.words.form.imageUrl')}</Label>
        <div className="flex items-start gap-3">
          <Input
            id="word-imageUrl"
            type="url"
            inputMode="url"
            autoComplete="off"
            placeholder="https://"
            maxLength={imageUrlMaxLength}
            aria-invalid={errors.imageUrl ? true : undefined}
            aria-describedby={errors.imageUrl ? 'word-imageUrl-error' : 'word-imageUrl-hint'}
            {...form.register('imageUrl')}
          />
          <WordPicture url={isHttpsUrl(imageUrl) ? imageUrl : null} alt={t('admin.content.words.form.preview')} />
        </div>
        {fieldError('imageUrl') ?? (
          <p id="word-imageUrl-hint" className="text-sm text-muted-foreground">
            {pictureRequired
              ? t('admin.content.words.form.imageUrlHintRequired')
              : t('admin.content.words.form.imageUrlHint')}
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
            : word
              ? t('admin.content.form.save')
              : t('admin.content.words.form.add')}
        </Button>
      </DialogFooter>
    </form>
  )
}

const serverFieldKeys: Record<'english' | 'meaning' | 'imageUrl', ParseKeys> = {
  english: 'admin.content.words.form.validation.englishRequired',
  meaning: 'admin.content.words.form.validation.meaningRequired',
  imageUrl: 'admin.content.words.form.validation.imageUrlHttps',
}
