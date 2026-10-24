import type { ParseKeys } from 'i18next'
import { Archive, ArrowDown, ArrowUp, CircleAlert, Eye, EyeOff } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { ContentStatus, ContentStatusAction, StatusChange } from '@/areas/admin/content/api'
import { ApiError } from '@/shared/api/problem-details'
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
import { Badge } from '@/shared/components/ui/badge'
import { Button } from '@/shared/components/ui/button'

const statusKeys: Record<ContentStatus, ParseKeys> = {
  Draft: 'admin.content.status.Draft',
  Published: 'admin.content.status.Published',
  Archived: 'admin.content.status.Archived',
}

export function ContentStatusBadge({ status }: { status: string }) {
  const { t } = useTranslation()
  const key = statusKeys[status as ContentStatus]
  const variant = status === 'Published' ? 'default' : status === 'Archived' ? 'outline' : 'secondary'
  return <Badge variant={variant}>{key ? t(key) : status}</Badge>
}

// The 422 rule codes of the backend (Domain/Exceptions/ContentErrorCodes.cs).
const ruleKeys: Record<string, ParseKeys> = {
  'content-archived': 'admin.content.errors.archived',
  'content-started': 'admin.content.errors.started',
  'lesson-structure-locked': 'admin.content.errors.structureLocked',
  'lesson-not-ready': 'admin.content.errors.notReady',
  'word-limit': 'admin.content.errors.wordLimit',
  'word-duplicate': 'admin.content.errors.duplicateWord',
  'word-in-use': 'admin.content.errors.wordInUse',
  'activity-limit': 'admin.content.errors.activityLimit',
  'question-limit': 'admin.content.errors.questionLimit',
  'question-invalid': 'admin.content.errors.questionInvalid',
  'matching-invalid': 'admin.content.errors.matchingInvalid',
  'boss-no-word-bank': 'admin.content.errors.bossNoWordBank',
  'boss-exists': 'admin.content.errors.bossExists',
  'level-needs-boss': 'admin.content.errors.levelNeedsBoss',
  'boss-required': 'admin.content.errors.bossRequired',
}

/**
 * What a failed content change means for the Admin. A 422 says why in its `code`; without one (an older API), the
 * item was most likely archived meanwhile, or, for unpublish, children have started it (PD-012).
 */
export function contentErrorKey(error: unknown, action?: ContentStatusAction): ParseKeys {
  if (!(error instanceof ApiError)) return 'admin.content.errors.failed'
  switch (error.kind) {
    case 'conflict':
      return 'admin.content.errors.conflict'
    case 'not-found':
      return 'admin.content.errors.notFound'
    case 'rule':
      return (
        (error.code ? ruleKeys[error.code] : undefined) ??
        (action === 'unpublish' ? 'admin.content.errors.started' : 'admin.content.errors.archived')
      )
    default:
      return 'admin.content.errors.failed'
  }
}

export function ContentError({ error, action }: { error: unknown; action?: ContentStatusAction }) {
  const { t } = useTranslation()
  return (
    <Alert variant="destructive">
      <CircleAlert aria-hidden />
      <AlertDescription>{t(contentErrorKey(error, action))}</AlertDescription>
    </Alert>
  )
}

interface StatusActionsProps {
  /** The item's name, for the archive confirmation. */
  title: string
  status: string
  version: string
  kind: 'level' | 'lesson'
  pending: boolean
  onChange: (change: StatusChange) => void
  size?: 'sm' | 'default'
}

/** Publish or unpublish, and archive after a confirmation that says it is final. Nothing for archived items. */
export function ContentStatusActions({
  title,
  status,
  version,
  kind,
  pending,
  onChange,
  size = 'sm',
}: StatusActionsProps) {
  const { t } = useTranslation()
  if (status === 'Archived') return null

  return (
    <span className="flex flex-wrap gap-2">
      {status === 'Draft' ? (
        <Button size={size} disabled={pending} onClick={() => onChange({ action: 'publish', version })}>
          <Eye aria-hidden />
          {t('admin.content.actions.publish')}
        </Button>
      ) : (
        <Button
          size={size}
          variant="outline"
          disabled={pending}
          onClick={() => onChange({ action: 'unpublish', version })}
        >
          <EyeOff aria-hidden />
          {t('admin.content.actions.unpublish')}
        </Button>
      )}
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button size={size} variant="ghost" disabled={pending}>
            <Archive aria-hidden />
            {t('admin.content.actions.archive')}
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('admin.content.actions.archiveTitle', { title })}</AlertDialogTitle>
            <AlertDialogDescription>
              {kind === 'level'
                ? t('admin.content.actions.archiveLevelDescription')
                : t('admin.content.actions.archiveLessonDescription')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('admin.content.actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={() => onChange({ action: 'archive', version })}>
              {t('admin.content.actions.archiveConfirm')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </span>
  )
}

interface MoveButtonsProps {
  title: string
  index: number
  count: number
  pending: boolean
  onMove: (offset: -1 | 1) => void
}

/** Up and down buttons to change the order (no drag and drop: works with touch and keyboard). */
export function MoveButtons({ title, index, count, pending, onMove }: MoveButtonsProps) {
  const { t } = useTranslation()
  return (
    <span className="flex gap-1">
      <Button
        size="icon-sm"
        variant="ghost"
        disabled={pending || index === 0}
        aria-label={t('admin.content.actions.moveUp', { title })}
        onClick={() => onMove(-1)}
      >
        <ArrowUp aria-hidden />
      </Button>
      <Button
        size="icon-sm"
        variant="ghost"
        disabled={pending || index === count - 1}
        aria-label={t('admin.content.actions.moveDown', { title })}
        onClick={() => onMove(1)}
      >
        <ArrowDown aria-hidden />
      </Button>
    </span>
  )
}
