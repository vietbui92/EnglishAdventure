import { ArrowLeft, CircleAlert, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import {
  moveInOrder,
  useAddWord,
  useLesson,
  useLessonActivities,
  useLessonStatus,
  useLessonWords,
  useRemoveWord,
  useReorderWords,
  useSetLearnCard,
  useUpdateWord,
  type LessonActivities,
  type LessonSummary,
  type LessonWord,
  type LessonWordBank,
} from '@/areas/admin/content/api'
import { ContentError, ContentStatusActions, ContentStatusBadge, MoveButtons } from '@/areas/admin/content/ContentParts'
import { ActivitiesEditor } from '@/areas/admin/content/ActivitiesEditor'
import { WordDialog } from '@/areas/admin/content/WordDialog'
import { WordPicture } from '@/areas/admin/content/WordPicture'
import { ApiError } from '@/shared/api/problem-details'
import { hasPermission, Permissions } from '@/shared/auth/permissions'
import { useSession } from '@/shared/auth/session'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/shared/components/ui/alert-dialog'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { Button } from '@/shared/components/ui/button'
import { Checkbox } from '@/shared/components/ui/checkbox'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'

function usePermissions() {
  const session = useSession()
  return {
    canManage: session ? hasPermission(session.user, Permissions.contentManage) : false,
    canPublish: session ? hasPermission(session.user, Permissions.contentPublish) : false,
  }
}

/** One lesson: its word bank and learn cards (S3.6, S3.7, F2.7) and its quizzes (S3.2, F2.5). */
export function LessonPage() {
  const { t } = useTranslation()
  const { lessonId = '' } = useParams()
  const lesson = useLesson(lessonId)
  const words = useLessonWords(lessonId)
  const activities = useLessonActivities(lessonId)

  const failed = lesson.error ?? words.error ?? activities.error
  return (
    <section className="flex flex-col gap-6">
      {lesson.data && (
        <Button asChild variant="ghost" size="sm" className="self-start">
          <Link to={`/admin/content/levels/${lesson.data.levelId}`}>
            <ArrowLeft aria-hidden />
            {t('admin.content.lesson.back')}
          </Link>
        </Button>
      )}

      {failed ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          {failed instanceof ApiError && failed.kind === 'not-found' ? (
            <p>{t('admin.content.lesson.notFound')}</p>
          ) : (
            <>
              <p>{t('admin.content.lesson.loadError')}</p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  void lesson.refetch()
                  void words.refetch()
                  void activities.refetch()
                }}
              >
                {t('admin.content.retry')}
              </Button>
            </>
          )}
        </div>
      ) : lesson.data && words.data && activities.data ? (
        <>
          <LessonHeader lesson={lesson.data} bank={words.data} activities={activities.data} />
          <WordBank lessonId={lessonId} bank={words.data} />
          <ActivitiesEditor lessonId={lessonId} words={words.data.words} activities={activities.data} />
        </>
      ) : (
        <p role="status" className="text-muted-foreground">
          {t('admin.content.loading')}
        </p>
      )}
    </section>
  )
}

interface LessonHeaderProps {
  lesson: LessonSummary
  bank: LessonWordBank
  activities: LessonActivities
}

function LessonHeader({ lesson, bank, activities }: LessonHeaderProps) {
  const { t } = useTranslation()
  const { canPublish } = usePermissions()
  const status = useLessonStatus(lesson.id)
  const missingPictures = bank.words.filter((word) => !word.imageUrl).length
  // What a draft still lacks before it can be published (the backend's lesson-not-ready rule), first problem first.
  const missing =
    bank.words.length === 0
      ? t('admin.content.lesson.needsWords')
      : missingPictures > 0
        ? t('admin.content.lesson.needsPictures', { count: missingPictures })
        : !bank.words.some((word) => word.showInLearnStep)
          ? t('admin.content.lesson.needsCards')
          : activities.activities.length === 0
            ? t('admin.content.lesson.needsActivity')
            : activities.activities.some((activity) => activity.questions.length === 0)
              ? t('admin.content.lesson.needsQuestions')
              : null

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="font-heading text-2xl font-semibold break-words">{lesson.title}</h1>
        <ContentStatusBadge status={lesson.status} />
      </div>

      {lesson.status === 'Draft' && missing && (
        <Alert>
          <CircleAlert aria-hidden />
          <AlertDescription>{missing}</AlertDescription>
        </Alert>
      )}
      {lesson.status === 'Published' && (
        <p role="note" className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
          {t('admin.content.lesson.publishedNote')}
        </p>
      )}
      {lesson.status === 'Archived' && (
        <p role="note" className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
          {t('admin.content.lesson.archivedNote')}
        </p>
      )}

      {canPublish && (
        <ContentStatusActions
          kind="lesson"
          title={lesson.title}
          status={lesson.status}
          version={lesson.version}
          pending={status.isPending}
          onChange={(change) => status.mutate(change)}
        />
      )}
      {status.isError && <ContentError error={status.error} action={status.variables?.action} />}
    </div>
  )
}

function WordBank({ lessonId, bank }: { lessonId: string; bank: LessonWordBank }) {
  const { t } = useTranslation()
  const { canManage } = usePermissions()
  const [dialog, setDialog] = useState<{ open: boolean; word?: LessonWord }>({ open: false })
  const add = useAddWord(lessonId)
  const update = useUpdateWord(lessonId)
  const remove = useRemoveWord(lessonId)
  const reorder = useReorderWords(lessonId)
  const learnCard = useSetLearnCard(lessonId)

  const maxWords = Number(bank.maxWords)
  const cards = bank.words.filter((word) => word.showInLearnStep).length
  const draft = bank.lessonStatus === 'Draft'
  const canCorrect = canManage && bank.lessonStatus !== 'Archived'
  // Adding, removing and reordering change the lesson's structure: drafts only (PD-012).
  const canRestructure = canManage && draft
  const full = bank.words.length >= maxWords
  const busy = remove.isPending || reorder.isPending || learnCard.isPending
  const failure = remove.error ?? reorder.error ?? learnCard.error

  const move = (index: number, offset: -1 | 1) => {
    const wordIds = moveInOrder(bank.words, index, offset)
    if (wordIds) reorder.mutate({ wordIds, version: bank.lessonVersion })
  }

  return (
    <section aria-labelledby="words-title" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <h2 id="words-title" className="text-lg font-semibold">
          {t('admin.content.words.title', { count: bank.words.length, max: maxWords })}
        </h2>
        {canRestructure && (
          <Button size="sm" disabled={full} onClick={() => setDialog({ open: true })}>
            <Plus aria-hidden />
            {t('admin.content.words.add')}
          </Button>
        )}
      </div>
      <p className="text-sm text-muted-foreground">
        {canRestructure && full
          ? t('admin.content.words.full', { max: maxWords })
          : t('admin.content.words.description')}
      </p>
      {bank.words.length > 0 && (
        <p className="text-sm text-muted-foreground">{t('admin.content.words.learnCards', { count: cards })}</p>
      )}

      {failure && <ContentError error={failure} />}

      {bank.words.length === 0 ? (
        <p className="text-muted-foreground">{t('admin.content.words.empty')}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">{t('admin.content.columns.order')}</TableHead>
              <TableHead className="w-16">{t('admin.content.words.columns.picture')}</TableHead>
              <TableHead>{t('admin.content.words.columns.english')}</TableHead>
              <TableHead>{t('admin.content.words.columns.meaning')}</TableHead>
              <TableHead>{t('admin.content.words.columns.learnCard')}</TableHead>
              {canCorrect && <TableHead>{t('admin.content.columns.actions')}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {bank.words.map((word, index) => (
              <TableRow key={word.id}>
                <TableCell>{index + 1}</TableCell>
                <TableCell>
                  <WordPicture url={word.imageUrl} alt={word.english} />
                </TableCell>
                <TableCell lang="en" className="font-medium whitespace-normal">
                  {word.english}
                </TableCell>
                <TableCell className="whitespace-normal">{word.meaning}</TableCell>
                <TableCell>
                  {/* Choosing the cards changes the lesson's structure: drafts only (owner, 2026-10-24). */}
                  {canRestructure ? (
                    <Checkbox
                      checked={word.showInLearnStep}
                      disabled={busy}
                      aria-label={t('admin.content.words.showInLearnStep', { word: word.english })}
                      onCheckedChange={(checked) =>
                        learnCard.mutate({ wordId: word.id, show: checked === true, version: bank.lessonVersion })
                      }
                    />
                  ) : (
                    <span className="text-sm text-muted-foreground">
                      {word.showInLearnStep
                        ? t('admin.content.words.learnCardShown')
                        : t('admin.content.words.learnCardHidden')}
                    </span>
                  )}
                </TableCell>
                {canCorrect && (
                  <TableCell>
                    <span className="flex flex-wrap items-center gap-1">
                      {canRestructure && (
                        <MoveButtons
                          title={word.english}
                          index={index}
                          count={bank.words.length}
                          pending={busy}
                          onMove={(offset) => move(index, offset)}
                        />
                      )}
                      <Button
                        size="icon-sm"
                        variant="ghost"
                        aria-label={t('admin.content.words.edit', { word: word.english })}
                        onClick={() => setDialog({ open: true, word })}
                      >
                        <Pencil aria-hidden />
                      </Button>
                      {canRestructure && (
                        <RemoveWordButton
                          word={word}
                          pending={busy}
                          onRemove={() => remove.mutate({ wordId: word.id, version: bank.lessonVersion })}
                        />
                      )}
                    </span>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <WordDialog
        open={dialog.open}
        onOpenChange={(open) => setDialog((current) => ({ ...current, open }))}
        word={dialog.word}
        pictureRequired={bank.lessonStatus === 'Published'}
        onSave={(values) =>
          dialog.word
            ? update.mutateAsync({ ...values, wordId: dialog.word.id, version: bank.lessonVersion })
            : add.mutateAsync({ ...values, version: bank.lessonVersion })
        }
      />
    </section>
  )
}

function RemoveWordButton({ word, pending, onRemove }: { word: LessonWord; pending: boolean; onRemove: () => void }) {
  const { t } = useTranslation()
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button
          size="icon-sm"
          variant="ghost"
          disabled={pending}
          aria-label={t('admin.content.words.remove', { word: word.english })}
        >
          <Trash2 aria-hidden />
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('admin.content.words.removeTitle', { word: word.english })}</AlertDialogTitle>
          <AlertDialogDescription>{t('admin.content.words.removeDescription')}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t('admin.content.actions.cancel')}</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onRemove}>
            {t('admin.content.words.removeConfirm')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
