import type { ParseKeys } from 'i18next'
import { Sparkles } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import {
  quizForms,
  suggestWrongWords,
  type LessonWord,
  type QuestionInput,
  type QuizForm,
  type QuizQuestion,
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
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/shared/components/ui/radio-group'
import { zodResolver } from '@/shared/lib/zod-resolver'

// Same rules as the backend QuizQuestionSpec (S3.2): 2–4 options, a fill-in sentence with exactly one gap.
const maxWrongWords = 3
const defaultWrongWords = 2
const sentenceMaxLength = 200
export const gap = '___'

export function isValidSentence(value: string) {
  const sentence = value.trim()
  const first = sentence.indexOf(gap)
  return (
    sentence.length > 0 &&
    sentence.length <= sentenceMaxLength &&
    first >= 0 &&
    first === sentence.lastIndexOf(gap) &&
    !sentence.includes(`${gap}_`)
  )
}

const questionSchema = z
  .object({
    form: z.enum(quizForms),
    correctWordId: z.string().min(1, 'admin.content.quiz.form.validation.correctRequired' satisfies ParseKeys),
    wrongWordIds: z
      .array(z.string())
      .min(1, 'admin.content.quiz.form.validation.wrongRequired' satisfies ParseKeys)
      .max(maxWrongWords, 'admin.content.quiz.form.validation.wrongTooMany' satisfies ParseKeys),
    sentence: z.string(),
  })
  .refine((values) => values.form !== 'FillInTheBlank' || isValidSentence(values.sentence), {
    path: ['sentence'],
    message: 'admin.content.quiz.form.validation.sentence' satisfies ParseKeys,
  })

type QuestionForm = z.input<typeof questionSchema>

interface QuestionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  words: LessonWord[]
  /** The question to change; without it the dialog adds a new one. */
  question?: QuizQuestion
  /** A published lesson: only a fill-in sentence can be corrected (PD-012). */
  sentenceOnly: boolean
  /** Saves the values; a rejection keeps the dialog open and shows the error. */
  onSave: (values: QuestionInput) => Promise<unknown>
}

/** Add or change a quiz question: its form, the correct word, 1–3 wrong words and, to fill in, a sentence. */
export function QuestionDialog(props: QuestionDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent showCloseButton={false} className="max-h-[90svh] overflow-y-auto">
        {/* Re-created on every opening, so the form starts from the question's current values. */}
        {props.open && <QuestionFormBody {...props} />}
      </DialogContent>
    </Dialog>
  )
}

function QuestionFormBody({ onOpenChange, words, question, sentenceOnly, onSave }: QuestionDialogProps) {
  const { t } = useTranslation()
  const [error, setError] = useState<unknown>(null)
  const firstWordId = words[0]?.id ?? ''
  const form = useForm<QuestionForm, unknown, z.output<typeof questionSchema>>({
    resolver: zodResolver(questionSchema),
    defaultValues: question
      ? {
          form: question.form as QuizForm,
          correctWordId: question.correctWordId,
          wrongWordIds: question.wrongWordIds,
          sentence: question.sentence ?? '',
        }
      : {
          form: 'PictureToWord',
          correctWordId: firstWordId,
          wrongWordIds: suggestWrongWords(words, firstWordId, defaultWrongWords),
          sentence: '',
        },
  })
  const { errors, isSubmitting } = form.formState
  const [selectedForm, correctWordId, wrongWordIds] = useWatch({
    control: form.control,
    name: ['form', 'correctWordId', 'wrongWordIds'],
  })
  const locked = sentenceOnly

  const suggest = (correct: string) =>
    form.setValue(
      'wrongWordIds',
      suggestWrongWords(words, correct, Math.max(wrongWordIds.length, defaultWrongWords)).slice(0, maxWrongWords),
      { shouldValidate: form.formState.isSubmitted },
    )

  const submit = form.handleSubmit(async (values) => {
    setError(null)
    // A published question keeps its structure; only the sentence is sent as typed (disabled fields may read empty).
    const structure =
      locked && question
        ? {
            form: question.form as QuizForm,
            correctWordId: question.correctWordId,
            wrongWordIds: question.wrongWordIds,
          }
        : values
    try {
      await onSave({
        form: structure.form,
        correctWordId: structure.correctWordId,
        wrongWordIds: structure.wrongWordIds,
        sentence: structure.form === 'FillInTheBlank' ? values.sentence.trim() : null,
      })
      onOpenChange(false)
    } catch (cause) {
      if (cause instanceof ApiError && cause.kind === 'validation') {
        // The server's messages are English; the form's own checks match its rules, so mark the likely field.
        form.setError(cause.fieldErrors.sentence ? 'sentence' : 'wrongWordIds', {
          type: 'server',
          message: cause.fieldErrors.sentence
            ? 'admin.content.quiz.form.validation.sentence'
            : 'admin.content.quiz.form.validation.wrongRequired',
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
          {question ? t('admin.content.quiz.form.editTitle') : t('admin.content.quiz.form.addTitle')}
        </DialogTitle>
        <DialogDescription>
          {locked ? t('admin.content.quiz.form.sentenceOnly') : t('admin.content.quiz.form.hint')}
        </DialogDescription>
      </DialogHeader>

      {error !== null && <ContentError error={error} />}

      <fieldset className="flex flex-col gap-2" disabled={locked}>
        <legend className="mb-2 text-sm font-medium">{t('admin.content.quiz.form.form')}</legend>
        <Controller
          control={form.control}
          name="form"
          render={({ field }) => (
            <RadioGroup value={field.value} onValueChange={field.onChange} disabled={locked} className="gap-2">
              {quizForms.map((value) => (
                <div key={value} className="flex items-start gap-3">
                  <RadioGroupItem id={`question-form-${value}`} value={value} />
                  <div className="grid gap-0.5">
                    <Label htmlFor={`question-form-${value}`}>{t(`admin.content.quiz.forms.${value}`)}</Label>
                    <span className="text-sm text-muted-foreground">{t(`admin.content.quiz.formHints.${value}`)}</span>
                  </div>
                </div>
              ))}
            </RadioGroup>
          )}
        />
      </fieldset>

      <div className="flex flex-col gap-2">
        <Label htmlFor="question-correct">{t('admin.content.quiz.form.correct')}</Label>
        {locked ? (
          <p id="question-correct" lang="en" className="text-sm font-medium">
            {words.find((word) => word.id === correctWordId)?.english}
          </p>
        ) : (
          <select
            id="question-correct"
            className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm"
            {...form.register('correctWordId', {
              onChange: (event: { target: { value: string } }) => {
                // A wrong option cannot also be the answer: keep the choice valid and suggest again.
                suggest(event.target.value)
              },
            })}
          >
            {words.map((word) => (
              <option key={word.id} value={word.id} lang="en">
                {word.english}
              </option>
            ))}
          </select>
        )}
        {message(errors.correctWordId?.message)}
      </div>

      <fieldset className="flex flex-col gap-2" disabled={locked}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <legend className="text-sm font-medium">{t('admin.content.quiz.form.wrong', { max: maxWrongWords })}</legend>
          {!locked && (
            <Button type="button" size="sm" variant="ghost" onClick={() => suggest(correctWordId)}>
              <Sparkles aria-hidden />
              {t('admin.content.quiz.form.suggest')}
            </Button>
          )}
        </div>
        <Controller
          control={form.control}
          name="wrongWordIds"
          render={({ field }) => (
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {words
                .filter((word) => word.id !== correctWordId)
                .map((word) => {
                  const checked = field.value.includes(word.id)
                  return (
                    <div key={word.id} className="flex items-center gap-2">
                      <Checkbox
                        id={`question-wrong-${word.id}`}
                        checked={checked}
                        disabled={locked || (!checked && field.value.length >= maxWrongWords)}
                        onCheckedChange={(next) =>
                          field.onChange(
                            next === true ? [...field.value, word.id] : field.value.filter((id) => id !== word.id),
                          )
                        }
                      />
                      <Label htmlFor={`question-wrong-${word.id}`} lang="en">
                        {word.english}
                      </Label>
                    </div>
                  )
                })}
            </div>
          )}
        />
        {message(errors.wrongWordIds?.message)}
      </fieldset>

      {selectedForm === 'FillInTheBlank' && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="question-sentence">{t('admin.content.quiz.form.sentence')}</Label>
          <Input
            id="question-sentence"
            lang="en"
            autoComplete="off"
            maxLength={sentenceMaxLength}
            placeholder={`I have a ${gap}.`}
            aria-invalid={errors.sentence ? true : undefined}
            {...form.register('sentence')}
          />
          {message(errors.sentence?.message) ?? (
            <p className="text-sm text-muted-foreground">{t('admin.content.quiz.form.sentenceHint', { gap })}</p>
          )}
        </div>
      )}

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
          {t('admin.content.actions.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting || (locked && selectedForm !== 'FillInTheBlank')}>
          {isSubmitting
            ? t('admin.content.form.saving')
            : question
              ? t('admin.content.form.save')
              : t('admin.content.quiz.form.add')}
        </Button>
      </DialogFooter>
    </form>
  )
}
