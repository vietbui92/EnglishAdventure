import { ArrowLeft, Pencil, Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import {
  moveInOrder,
  useCreateLesson,
  useLessonStatus,
  useLevel,
  useLevelStatus,
  useReorderLessons,
  useUpdateLesson,
  useUpdateLevel,
  type LessonSummary,
  type LevelSummary,
} from '@/areas/admin/content/api'
import { ContentFormDialog } from '@/areas/admin/content/ContentFormDialog'
import { ContentError, ContentStatusActions, ContentStatusBadge, MoveButtons } from '@/areas/admin/content/ContentParts'
import { ApiError } from '@/shared/api/problem-details'
import { hasPermission, Permissions } from '@/shared/auth/permissions'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'

function usePermissions() {
  const session = useSession()
  return {
    canManage: session ? hasPermission(session.user, Permissions.contentManage) : false,
    canPublish: session ? hasPermission(session.user, Permissions.contentPublish) : false,
  }
}

/** One level with its lessons (S3.1, F2.4). An archived level is read-only (PD-012). */
export function LevelDetailPage() {
  const { t } = useTranslation()
  const { levelId = '' } = useParams()
  const level = useLevel(levelId)

  return (
    <section className="flex flex-col gap-6">
      <Button asChild variant="ghost" size="sm" className="self-start">
        <Link to="/admin/content">
          <ArrowLeft aria-hidden />
          {t('admin.content.level.back')}
        </Link>
      </Button>

      {level.isPending ? (
        <p role="status" className="text-muted-foreground">
          {t('admin.content.loading')}
        </p>
      ) : level.isError ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          {level.error instanceof ApiError && level.error.kind === 'not-found' ? (
            <p>{t('admin.content.level.notFound')}</p>
          ) : (
            <>
              <p>{t('admin.content.level.loadError')}</p>
              <Button variant="outline" size="sm" onClick={() => void level.refetch()}>
                {t('admin.content.retry')}
              </Button>
            </>
          )}
        </div>
      ) : (
        <>
          <LevelHeader level={level.data.level} />
          <LessonsSection level={level.data.level} lessons={level.data.lessons} />
        </>
      )}
    </section>
  )
}

function LevelHeader({ level }: { level: LevelSummary }) {
  const { t } = useTranslation()
  const { canManage, canPublish } = usePermissions()
  const [editing, setEditing] = useState(false)
  const update = useUpdateLevel(level.id)
  const status = useLevelStatus(level.id)
  const archived = level.status === 'Archived'

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="font-heading text-2xl font-semibold break-words">{level.title}</h1>
        <ContentStatusBadge status={level.status} />
      </div>
      {level.description && <p className="max-w-prose whitespace-pre-line">{level.description}</p>}
      {archived && (
        <p role="note" className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
          {t('admin.content.level.archivedNote')}
        </p>
      )}

      {!archived && (canManage || canPublish) && (
        <div className="flex flex-wrap gap-2">
          {canManage && (
            <Button size="sm" variant="outline" onClick={() => setEditing(true)}>
              <Pencil aria-hidden />
              {t('admin.content.level.edit')}
            </Button>
          )}
          {canPublish && (
            <ContentStatusActions
              kind="level"
              title={level.title}
              status={level.status}
              version={level.version}
              pending={status.isPending}
              onChange={(change) => status.mutate(change)}
            />
          )}
        </div>
      )}
      {status.isError && <ContentError error={status.error} action={status.variables?.action} />}

      <ContentFormDialog
        open={editing}
        onOpenChange={setEditing}
        heading={t('admin.content.level.editTitle')}
        submitLabel={t('admin.content.form.save')}
        withDescription
        initial={{ title: level.title, description: level.description ?? '' }}
        onSave={(values) => update.mutateAsync({ ...values, version: level.version })}
      />
    </div>
  )
}

function LessonsSection({ level, lessons }: { level: LevelSummary; lessons: LessonSummary[] }) {
  const { t } = useTranslation()
  const { canManage, canPublish } = usePermissions()
  const [creating, setCreating] = useState(false)
  const create = useCreateLesson(level.id)
  const reorder = useReorderLessons(level.id)
  const readOnly = level.status === 'Archived'
  const ordered = lessons.filter((lesson) => lesson.status !== 'Archived')

  const move = (index: number, offset: -1 | 1) => {
    const lessonIds = moveInOrder(ordered, index, offset)
    if (lessonIds) reorder.mutate(lessonIds)
  }

  return (
    <section aria-labelledby="lessons-title" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <h2 id="lessons-title" className="text-lg font-semibold">
          {t('admin.content.lessons.title')}
        </h2>
        {canManage && !readOnly && (
          <Button size="sm" onClick={() => setCreating(true)}>
            <Plus aria-hidden />
            {t('admin.content.lessons.add')}
          </Button>
        )}
      </div>
      <p className="text-sm text-muted-foreground">{t('admin.content.lessons.contentLater')}</p>

      {reorder.isError && <ContentError error={reorder.error} />}

      {lessons.length === 0 ? (
        <p className="text-muted-foreground">{t('admin.content.lessons.empty')}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">{t('admin.content.columns.order')}</TableHead>
              <TableHead>{t('admin.content.columns.title')}</TableHead>
              <TableHead>{t('admin.content.columns.status')}</TableHead>
              {!readOnly && (canManage || canPublish) && <TableHead>{t('admin.content.columns.actions')}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {lessons.map((lesson) => {
              const index = ordered.indexOf(lesson)
              return (
                <LessonRow
                  key={lesson.id}
                  lesson={lesson}
                  index={index}
                  count={ordered.length}
                  readOnly={readOnly}
                  reordering={reorder.isPending}
                  onMove={(offset) => move(index, offset)}
                />
              )
            })}
          </TableBody>
        </Table>
      )}

      <ContentFormDialog
        open={creating}
        onOpenChange={setCreating}
        heading={t('admin.content.lessons.createTitle')}
        submitLabel={t('admin.content.form.create')}
        withDescription={false}
        onSave={(values) => create.mutateAsync({ title: values.title })}
      />
    </section>
  )
}

interface LessonRowProps {
  lesson: LessonSummary
  /** The position among the non-archived lessons, −1 for an archived one. */
  index: number
  count: number
  readOnly: boolean
  reordering: boolean
  onMove: (offset: -1 | 1) => void
}

function LessonRow({ lesson, index, count, readOnly, reordering, onMove }: LessonRowProps) {
  const { t } = useTranslation()
  const { canManage, canPublish } = usePermissions()
  const [editing, setEditing] = useState(false)
  const update = useUpdateLesson(lesson.id)
  const status = useLessonStatus(lesson.id)
  const archived = lesson.status === 'Archived'
  const showActions = !readOnly && (canManage || canPublish)

  return (
    <TableRow>
      <TableCell>{archived ? '–' : index + 1}</TableCell>
      <TableCell className="whitespace-normal">
        <Link to={`/admin/content/lessons/${lesson.id}`} className="font-medium underline-offset-4 hover:underline">
          {lesson.title}
        </Link>
        {status.isError && (
          <div className="mt-2">
            <ContentError error={status.error} action={status.variables?.action} />
          </div>
        )}
      </TableCell>
      <TableCell>
        <ContentStatusBadge status={lesson.status} />
      </TableCell>
      {showActions && (
        <TableCell>
          {!archived && (
            <span className="flex flex-wrap items-center gap-2">
              {canManage && (
                <>
                  <MoveButtons title={lesson.title} index={index} count={count} pending={reordering} onMove={onMove} />
                  <Button
                    size="icon-sm"
                    variant="ghost"
                    aria-label={t('admin.content.lessons.rename', { title: lesson.title })}
                    onClick={() => setEditing(true)}
                  >
                    <Pencil aria-hidden />
                  </Button>
                </>
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
            </span>
          )}
          <ContentFormDialog
            open={editing}
            onOpenChange={setEditing}
            heading={t('admin.content.lessons.renameTitle')}
            submitLabel={t('admin.content.form.save')}
            withDescription={false}
            initial={{ title: lesson.title }}
            onSave={(values) => update.mutateAsync({ title: values.title, version: lesson.version })}
          />
        </TableCell>
      )}
    </TableRow>
  )
}
