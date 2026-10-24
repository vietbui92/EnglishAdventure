import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { moveInOrder, useCreateLevel, useLevels, useReorderLevels } from '@/areas/admin/content/api'
import { ContentFormDialog } from '@/areas/admin/content/ContentFormDialog'
import { ContentError, ContentStatusBadge, MoveButtons } from '@/areas/admin/content/ContentParts'
import { hasPermission, Permissions } from '@/shared/auth/permissions'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'
import { Checkbox } from '@/shared/components/ui/checkbox'
import { Label } from '@/shared/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'

/** The levels of the adventure in their order (S3.1, F2.4). */
export function LevelsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [includeArchived, setIncludeArchived] = useState(false)
  const [creating, setCreating] = useState(false)
  const levels = useLevels(includeArchived)
  const create = useCreateLevel()
  const reorder = useReorderLevels()
  const session = useSession()
  const canManage = session ? hasPermission(session.user, Permissions.contentManage) : false

  const ordered = levels.data?.filter((level) => level.status !== 'Archived') ?? []

  const move = (index: number, offset: -1 | 1) => {
    const levelIds = moveInOrder(ordered, index, offset)
    if (levelIds) reorder.mutate(levelIds)
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-heading text-2xl font-semibold">{t('admin.content.levels.title')}</h1>
          {canManage && (
            <Button size="sm" onClick={() => setCreating(true)}>
              <Plus aria-hidden />
              {t('admin.content.levels.add')}
            </Button>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Checkbox
            id="show-archived-levels"
            checked={includeArchived}
            onCheckedChange={(checked) => setIncludeArchived(checked === true)}
          />
          <Label htmlFor="show-archived-levels">{t('admin.content.levels.showArchived')}</Label>
        </div>
      </div>
      <p className="text-sm text-muted-foreground">{t('admin.content.levels.description')}</p>

      {reorder.isError && <ContentError error={reorder.error} />}

      {levels.isPending ? (
        <p role="status" className="text-muted-foreground">
          {t('admin.content.loading')}
        </p>
      ) : levels.isError ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          <p>{t('admin.content.levels.loadError')}</p>
          <Button variant="outline" size="sm" onClick={() => void levels.refetch()}>
            {t('admin.content.retry')}
          </Button>
        </div>
      ) : levels.data.length === 0 ? (
        <p className="text-muted-foreground">{t('admin.content.levels.empty')}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">{t('admin.content.columns.order')}</TableHead>
              <TableHead>{t('admin.content.columns.title')}</TableHead>
              <TableHead>{t('admin.content.columns.status')}</TableHead>
              <TableHead>{t('admin.content.columns.lessons')}</TableHead>
              {canManage && <TableHead className="w-24">{t('admin.content.columns.move')}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {levels.data.map((level) => {
              const index = ordered.indexOf(level)
              const lessons = level.lessons
              return (
                <TableRow key={level.id}>
                  <TableCell>{index >= 0 ? index + 1 : '–'}</TableCell>
                  <TableCell className="whitespace-normal">
                    <Link to={`levels/${level.id}`} className="font-medium underline-offset-4 hover:underline">
                      {level.title}
                    </Link>
                  </TableCell>
                  <TableCell>
                    <ContentStatusBadge status={level.status} />
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {t('admin.content.levels.lessonCounts', {
                      published: Number(lessons.published),
                      draft: Number(lessons.draft),
                    })}
                  </TableCell>
                  {canManage && (
                    <TableCell>
                      {index >= 0 && (
                        <MoveButtons
                          title={level.title}
                          index={index}
                          count={ordered.length}
                          pending={reorder.isPending}
                          onMove={(offset) => move(index, offset)}
                        />
                      )}
                    </TableCell>
                  )}
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      )}

      <ContentFormDialog
        open={creating}
        onOpenChange={setCreating}
        heading={t('admin.content.levels.createTitle')}
        submitLabel={t('admin.content.form.create')}
        withDescription
        onSave={async (values) => {
          const created = await create.mutateAsync(values)
          await navigate(`levels/${created.level.id}`)
        }}
      />
    </section>
  )
}
