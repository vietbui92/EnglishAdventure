import type { ParseKeys } from 'i18next'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import {
  matchingModes,
  type LessonActivity,
  type LessonWord,
  type MatchingInput,
  type MatchingMode,
} from '@/areas/admin/content/api'
import { ContentError } from '@/areas/admin/content/ContentParts'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/components/ui/button'
import { Checkbox } from '@/shared/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'
import { Label } from '@/shared/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/shared/components/ui/radio-group'
import { zodResolver } from '@/shared/lib/zod-resolver'

// Same limits as the backend MatchingActivity (S3.3, owner 2026-10-24).
export const minPairs = 2
const maxPairs = 6
const defaultPairs = 4

const matchingSchema = z.object({
  mode: z.enum(matchingModes),
  wordIds: z
    .array(z.string())
    .min(minPairs, 'admin.content.matching.form.validation.tooFew' satisfies ParseKeys)
    .max(maxPairs, 'admin.content.matching.form.validation.tooMany' satisfies ParseKeys),
})

type MatchingForm = z.input<typeof matchingSchema>

interface MatchingDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  words: LessonWord[]
  /** The matching to change; without it the dialog adds a new one. */
  matching?: LessonActivity
  /** Saves the values; a rejection keeps the dialog open and shows the error. */
  onSave: (values: MatchingInput) => Promise<unknown>
}

/** Add or change a matching: word ↔ picture or word ↔ meaning, with 2–6 words of the lesson. */
export function MatchingDialog(props: MatchingDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false} className="max-h-[90svh] overflow-y-auto">
        {/* Re-created on every opening, so the form starts from the matching's current values. */}
        {props.open && <MatchingFormBody {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function MatchingFormBody({ onOpenChange, words, matching, onSave }: MatchingDialogProps) {
  const { t } = useTranslation()
  const [error, setError] = useState<unknown>(null)
  const form = useForm<MatchingForm, unknown, z.output<typeof matchingSchema>>({
    resolver: zodResolver(matchingSchema),
    defaultValues: matching
      ? { mode: (matching.matchingMode ?? 'WordToPicture') as MatchingMode, wordIds: matching.pairWordIds }
      : { mode: 'WordToPicture', wordIds: words.slice(0, defaultPairs).map((word) => word.id) },
  })
  const { errors, isSubmitting } = form.formState

  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try {
      // The pairs keep the word bank's order.
      const chosen = new Set(values.wordIds)
      await onSave({ mode: values.mode, wordIds: words.filter((word) => chosen.has(word.id)).map((word) => word.id) })
      onOpenChange(false)
    } catch (cause) {
      if (cause instanceof ApiError && cause.kind === 'validation') {
        form.setError('wordIds', { type: 'server', message: 'admin.content.matching.form.validation.tooFew' })
      } else {
        setError(cause)
      }
    }
  })

  return (
    <form noValidate onSubmit={(event) => void submit(event)} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>
          {matching ? t('admin.content.matching.form.editTitle') : t('admin.content.matching.form.addTitle')}
        </DialogTitle>
        <DialogDescription>{t('admin.content.matching.form.hint', { min: minPairs, max: maxPairs })}</DialogDescription>
      </DialogHeader>

      {error !== null && <ContentError error={error} />}

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium">{t('admin.content.matching.form.mode')}</legend>
        <Controller
          control={form.control}
          name="mode"
          render={({ field }) => (
            <RadioGroup value={field.value} onValueChange={field.onChange} className="gap-2">
              {matchingModes.map((value) => (
                <div key={value} className="flex items-start gap-3">
                  <RadioGroupItem id={`matching-mode-${value}`} value={value} />
                  <div className="grid gap-0.5">
                    <Label htmlFor={`matching-mode-${value}`}>{t(`admin.content.matching.modes.${value}`)}</Label>
                    <span className="text-sm text-muted-foreground">
                      {t(`admin.content.matching.modeHints.${value}`)}
                    </span>
                  </div>
                </div>
              ))}
            </RadioGroup>
          )}
        />
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium">
          {t('admin.content.matching.form.words', { max: maxPairs })}
        </legend>
        <Controller
          control={form.control}
          name="wordIds"
          render={({ field }) => (
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {words.map((word) => {
                const checked = field.value.includes(word.id)
                return (
                  <div key={word.id} className="flex items-center gap-2">
                    <Checkbox
                      id={`matching-word-${word.id}`}
                      checked={checked}
                      disabled={!checked && field.value.length >= maxPairs}
                      onCheckedChange={(next) =>
                        field.onChange(
                          next === true ? [...field.value, word.id] : field.value.filter((id) => id !== word.id),
                        )
                      }
                    />
                    <Label htmlFor={`matching-word-${word.id}`} lang="en">
                      {word.english}
                    </Label>
                  </div>
                )
              })}
            </div>
          )}
        />
        {errors.wordIds?.message && (
          <p className="text-sm text-destructive">{t(errors.wordIds.message as ParseKeys)}</p>
        )}
      </fieldset>

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
          {t('admin.content.actions.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting
            ? t('admin.content.form.saving')
            : matching
              ? t('admin.content.form.save')
              : t('admin.content.matching.form.add')}
        </Button>
      </DialogFooter>
    </form>
  )
}
