import type { ParseKeys } from 'i18next'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  moveInOrder,
  useAddMatching,
  useAddQuestion,
  useAddQuiz,
  useRemoveActivity,
  useRemoveQuestion,
  useReorderActivities,
  useReorderQuestions,
  useUpdateMatching,
  useUpdateQuestion,
  type LessonActivities,
  type LessonActivity,
  type LessonWord,
  type QuizQuestion,
} from '@/areas/admin/content/api'
import { ContentError, MoveButtons } from '@/areas/admin/content/ContentParts'
import { MatchingDialog } from '@/areas/admin/content/MatchingDialog'
import { QuestionDialog } from '@/areas/admin/content/QuestionDialog'
import { WordPicture } from '@/areas/admin/content/WordPicture'
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
import { Button } from '@/shared/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'

/** Both games need two words: a quiz its answer and a wrong option, a matching two pairs (S3.2, S3.3). */
const minWordsForActivity = 2

const formKeys: Record<string, ParseKeys> = {
  PictureToWord: 'admin.content.quiz.forms.PictureToWord',
  WordToPicture: 'admin.content.quiz.forms.WordToPicture',
  FillInTheBlank: 'admin.content.quiz.forms.FillInTheBlank',
}

const modeKeys: Record<string, ParseKeys> = {
  WordToPicture: 'admin.content.matching.modes.WordToPicture',
  WordToMeaning: 'admin.content.matching.modes.WordToMeaning',
}

interface ActivitiesEditorProps {
  lessonId: string
  words: LessonWord[]
  activities: LessonActivities
}

/** The lesson's activities (S3.2, S3.3, F2.5): quizzes and matchings built from the word bank. */
export function ActivitiesEditor({ lessonId, words, activities }: ActivitiesEditorProps) {
  const { t } = useTranslation()
  const session = useSession()
  const canManage = session ? hasPermission(session.user, Permissions.contentManage) : false
  const [addingMatching, setAddingMatching] = useState(false)
  const addQuiz = useAddQuiz(lessonId)
  const addMatching = useAddMatching(lessonId)
  const reorder = useReorderActivities(lessonId)

  const draft = activities.lessonStatus === 'Draft'
  // Adding, removing and reordering change the lesson's structure: drafts only (PD-012).
  const canRestructure = canManage && draft
  const maxActivities = Number(activities.maxActivities)
  const list = activities.activities
  const enoughWords = words.length >= minWordsForActivity
  const canAdd = enoughWords && list.length < maxActivities
  const failure = addQuiz.error ?? reorder.error

  const move = (index: number, offset: -1 | 1) => {
    const activityIds = moveInOrder(list, index, offset)
    if (activityIds) reorder.mutate({ activityIds, version: activities.lessonVersion })
  }

  return (
    <section aria-labelledby="activities-title" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <h2 id="activities-title" className="text-lg font-semibold">
          {t('admin.content.quiz.title', { count: list.length, max: maxActivities })}
        </h2>
        {canRestructure && (
          <>
            <Button
              size="sm"
              disabled={!canAdd || addQuiz.isPending}
              onClick={() => addQuiz.mutate(activities.lessonVersion)}
            >
              <Plus aria-hidden />
              {t('admin.content.quiz.add')}
            </Button>
            <Button size="sm" variant="outline" disabled={!canAdd} onClick={() => setAddingMatching(true)}>
              <Plus aria-hidden />
              {t('admin.content.matching.add')}
            </Button>
          </>
        )}
      </div>
      <p className="text-sm text-muted-foreground">
        {canRestructure && !enoughWords
          ? t('admin.content.quiz.needsWords', { min: minWordsForActivity })
          : canRestructure && list.length >= maxActivities
            ? t('admin.content.quiz.full', { max: maxActivities })
            : t('admin.content.quiz.description')}
      </p>

      {failure && <ContentError error={failure} />}

      {list.length === 0 ? (
        <p className="text-muted-foreground">{t('admin.content.quiz.empty')}</p>
      ) : (
        list.map((activity, index) => {
          const shared = {
            lessonId,
            activity,
            index,
            count: list.length,
            words,
            version: activities.lessonVersion,
            canManage,
            canRestructure,
            reordering: reorder.isPending,
            onMove: (offset: -1 | 1) => move(index, offset),
          }
          return activity.type === 'Matching' ? (
            <MatchingCard key={activity.id} {...shared} />
          ) : (
            <QuizCard
              key={activity.id}
              {...shared}
              maxQuestions={Number(activities.maxQuestionsPerQuiz)}
              published={activities.lessonStatus === 'Published'}
            />
          )
        })
      )}

      <MatchingDialog
        open={addingMatching}
        onOpenChange={setAddingMatching}
        words={words}
        onSave={(values) => addMatching.mutateAsync({ ...values, version: activities.lessonVersion })}
      />
    </section>
  )
}

interface ActivityCardProps {
  lessonId: string
  activity: LessonActivity
  index: number
  count: number
  words: LessonWord[]
  version: string
  canManage: boolean
  canRestructure: boolean
  reordering: boolean
  onMove: (offset: -1 | 1) => void
}

/** ↑ / ↓ and remove-with-confirmation for one activity card. Drafts only. */
function ActivityActions({
  name,
  removeDescription,
  removeConfirm,
  ...props
}: Pick<ActivityCardProps, 'lessonId' | 'activity' | 'index' | 'count' | 'version' | 'reordering' | 'onMove'> & {
  name: string
  removeDescription: string
  removeConfirm: string
}) {
  const { t } = useTranslation()
  const remove = useRemoveActivity(props.lessonId)

  return (
    <span className="ml-auto flex flex-col items-end gap-1">
      <span className="flex flex-wrap items-center gap-1">
        <MoveButtons
          title={name}
          index={props.index}
          count={props.count}
          pending={props.reordering}
          onMove={props.onMove}
        />
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button
              size="icon-sm"
              variant="ghost"
              disabled={remove.isPending}
              aria-label={t('admin.content.quiz.remove', { name })}
            >
              <Trash2 aria-hidden />
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('admin.content.quiz.removeTitle', { name })}</AlertDialogTitle>
              <AlertDialogDescription>{removeDescription}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t('admin.content.actions.cancel')}</AlertDialogCancel>
              <AlertDialogAction
                variant="destructive"
                onClick={() => remove.mutate({ activityId: props.activity.id, version: props.version })}
              >
                {removeConfirm}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </span>
      {remove.isError && <ContentError error={remove.error} />}
    </span>
  )
}

function QuizCard(props: ActivityCardProps & { maxQuestions: number; published: boolean }) {
  const { lessonId, activity, index, words, version, maxQuestions, canManage, canRestructure, published } = props
  const { t } = useTranslation()
  const [dialog, setDialog] = useState<{ open: boolean; question?: QuizQuestion }>({ open: false })
  const addQuestion = useAddQuestion(lessonId)
  const updateQuestion = useUpdateQuestion(lessonId)
  const removeQuestion = useRemoveQuestion(lessonId)
  const reorderQuestions = useReorderQuestions(lessonId)

  const name = t('admin.content.quiz.name', { number: index + 1 })
  const questions = activity.questions
  const busy = removeQuestion.isPending || reorderQuestions.isPending
  const failure = removeQuestion.error ?? reorderQuestions.error
  const wordById = new Map(words.map((word) => [word.id, word]))
  // In a published lesson only a fill-in sentence can still be corrected (PD-012).
  const canEdit = (question: QuizQuestion) =>
    canManage && (canRestructure || (published && question.form === 'FillInTheBlank'))

  const moveQuestion = (questionIndex: number, offset: -1 | 1) => {
    const questionIds = moveInOrder(questions, questionIndex, offset)
    if (questionIds) reorderQuestions.mutate({ activityId: activity.id, questionIds, version })
  }

  return (
    <article aria-label={name} className="flex flex-col gap-3 rounded-lg border p-4">
      <div className="flex flex-wrap items-start gap-2">
        <h3 className="font-semibold">{name}</h3>
        <span className="text-sm text-muted-foreground">
          {t('admin.content.quiz.questionCount', { count: questions.length, max: maxQuestions })}
        </span>
        {canRestructure && (
          <ActivityActions
            {...props}
            name={name}
            removeDescription={t('admin.content.quiz.removeDescription')}
            removeConfirm={t('admin.content.quiz.removeConfirm')}
          />
        )}
      </div>

      {failure && <ContentError error={failure} />}

      {questions.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t('admin.content.quiz.noQuestions')}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">{t('admin.content.columns.order')}</TableHead>
              <TableHead>{t('admin.content.quiz.columns.form')}</TableHead>
              <TableHead>{t('admin.content.quiz.columns.prompt')}</TableHead>
              <TableHead>{t('admin.content.quiz.columns.correct')}</TableHead>
              <TableHead>{t('admin.content.quiz.columns.wrong')}</TableHead>
              {canManage && <TableHead>{t('admin.content.columns.actions')}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {questions.map((question, questionIndex) => {
              const correct = wordById.get(question.correctWordId)
              const label = t('admin.content.quiz.questionName', { number: questionIndex + 1 })
              return (
                <TableRow key={question.id}>
                  <TableCell>{questionIndex + 1}</TableCell>
                  <TableCell className="whitespace-normal">
                    {formKeys[question.form] ? t(formKeys[question.form]!) : question.form}
                  </TableCell>
                  <TableCell className="whitespace-normal">
                    <QuestionPrompt question={question} correct={correct} />
                  </TableCell>
                  <TableCell lang="en" className="font-medium">
                    {correct?.english}
                  </TableCell>
                  <TableCell lang="en" className="whitespace-normal">
                    {question.wrongWordIds.map((id) => wordById.get(id)?.english).join(', ')}
                  </TableCell>
                  {canManage && (
                    <TableCell>
                      <span className="flex flex-wrap items-center gap-1">
                        {canRestructure && (
                          <MoveButtons
                            title={label}
                            index={questionIndex}
                            count={questions.length}
                            pending={busy}
                            onMove={(offset) => moveQuestion(questionIndex, offset)}
                          />
                        )}
                        {canEdit(question) && (
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            aria-label={t('admin.content.quiz.editQuestion', { name: label })}
                            onClick={() => setDialog({ open: true, question })}
                          >
                            <Pencil aria-hidden />
                          </Button>
                        )}
                        {canRestructure && (
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            disabled={busy}
                            aria-label={t('admin.content.quiz.removeQuestion', { name: label })}
                            onClick={() =>
                              removeQuestion.mutate({ activityId: activity.id, questionId: question.id, version })
                            }
                          >
                            <Trash2 aria-hidden />
                          </Button>
                        )}
                      </span>
                    </TableCell>
                  )}
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      )}

      {canRestructure && (
        <Button
          size="sm"
          variant="outline"
          className="self-start"
          disabled={questions.length >= maxQuestions || words.length < minWordsForActivity}
          onClick={() => setDialog({ open: true })}
        >
          <Plus aria-hidden />
          {t('admin.content.quiz.addQuestion', { name })}
        </Button>
      )}

      <QuestionDialog
        open={dialog.open}
        onOpenChange={(open) => setDialog((current) => ({ ...current, open }))}
        words={words}
        question={dialog.question}
        sentenceOnly={!canRestructure}
        onSave={(values) =>
          dialog.question
            ? updateQuestion.mutateAsync({
                ...values,
                activityId: activity.id,
                questionId: dialog.question.id,
                version,
              })
            : addQuestion.mutateAsync({ ...values, activityId: activity.id, version })
        }
      />
    </article>
  )
}

/** A matching (S3.3): the words to match, each with its picture or meaning. Locked once published. */
function MatchingCard(props: ActivityCardProps) {
  const { lessonId, activity, index, words, version, canRestructure } = props
  const { t } = useTranslation()
  const [editing, setEditing] = useState(false)
  const update = useUpdateMatching(lessonId)

  const name = t('admin.content.matching.name', { number: index + 1 })
  const mode = activity.matchingMode ?? 'WordToPicture'
  const wordById = new Map(words.map((word) => [word.id, word]))

  return (
    <article aria-label={name} className="flex flex-col gap-3 rounded-lg border p-4">
      <div className="flex flex-wrap items-start gap-2">
        <h3 className="font-semibold">{name}</h3>
        <span className="text-sm text-muted-foreground">
          {modeKeys[mode] ? t(modeKeys[mode]!) : mode} ·{' '}
          {t('admin.content.matching.pairCount', { count: activity.pairWordIds.length })}
        </span>
        {canRestructure && (
          <ActivityActions
            {...props}
            name={name}
            removeDescription={t('admin.content.matching.removeDescription')}
            removeConfirm={t('admin.content.matching.removeConfirm')}
          />
        )}
      </div>

      <ul className="flex flex-wrap gap-2">
        {activity.pairWordIds.map((wordId) => {
          const word = wordById.get(wordId)
          return (
            <li key={wordId} className="flex items-center gap-2 rounded-md border px-2 py-1">
              <span lang="en" className="font-medium">
                {word?.english}
              </span>
              <span aria-hidden>↔</span>
              {mode === 'WordToPicture' ? (
                <WordPicture url={word?.imageUrl ?? null} alt={word?.english ?? ''} />
              ) : (
                <span>{word?.meaning}</span>
              )}
            </li>
          )
        })}
      </ul>

      {canRestructure && (
        <Button size="sm" variant="outline" className="self-start" onClick={() => setEditing(true)}>
          <Pencil aria-hidden />
          {t('admin.content.matching.edit', { name })}
        </Button>
      )}

      <MatchingDialog
        open={editing}
        onOpenChange={setEditing}
        words={words}
        matching={activity}
        onSave={(values) => update.mutateAsync({ ...values, activityId: activity.id, version })}
      />
    </article>
  )
}

/** What the child is shown: the picture, the English word, or the sentence with its gap. */
function QuestionPrompt({ question, correct }: { question: QuizQuestion; correct: LessonWord | undefined }) {
  switch (question.form) {
    case 'PictureToWord':
      return <WordPicture url={correct?.imageUrl ?? null} alt={correct?.english ?? ''} />
    case 'WordToPicture':
      return <span lang="en">{correct?.english}</span>
    default:
      return <span lang="en">{question.sentence}</span>
  }
}
