import { Archive, Pencil, Plus, RotateCcw } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  useArchiveChildProfile,
  useChildProfileOptions,
  useChildProfiles,
  useRestoreChildProfile,
  type ChildProfile,
} from '@/areas/parent/children/api'
import { ChildProfileDialog } from '@/areas/parent/children/ChildProfileDialog'
import { ApiError } from '@/shared/api/problem-details'
import { ChildAvatar, avatarKeys as drawnAvatars } from '@/shared/components/ChildAvatar'
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

// Until the options arrive (and if they fail), the page uses the backend's current values.
const fallbackMaxActive = 3

/** The Parent's child profiles (S2.1, F3.1): several per family, at most a few active, archived ones restorable. */
export function ChildrenPage() {
  const { t } = useTranslation()
  const profiles = useChildProfiles()
  const options = useChildProfileOptions()
  const maxActive = Number(options.data?.maxActive ?? fallbackMaxActive)
  const avatarKeys = options.data?.avatarKeys ?? drawnAvatars

  const [dialog, setDialog] = useState<{ open: boolean; profile?: ChildProfile }>({ open: false })

  if (profiles.isPending) {
    return (
      <p role="status" className="text-muted-foreground">
        {t('parent.children.loading')}
      </p>
    )
  }
  if (profiles.isError) {
    return (
      <div role="alert" className="flex flex-wrap items-center gap-3">
        <p>{t('parent.children.loadError')}</p>
        <Button variant="outline" size="sm" onClick={() => void profiles.refetch()}>
          {t('parent.children.retry')}
        </Button>
      </div>
    )
  }

  const active = profiles.data.filter((profile) => profile.status === 'Active')
  const archived = profiles.data.filter((profile) => profile.status === 'Archived')
  const atLimit = active.length >= maxActive

  return (
    <section className="flex flex-col gap-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-heading text-2xl font-semibold">{t('parent.children.title')}</h1>
          <p className="mt-1 text-muted-foreground">{t('parent.children.subtitle')}</p>
        </div>
        <div className="flex flex-col items-end gap-1">
          <Button disabled={atLimit} onClick={() => setDialog({ open: true })}>
            <Plus aria-hidden />
            {t('parent.children.add')}
          </Button>
          <span className="text-sm text-muted-foreground">
            {t('parent.children.count', { count: active.length, max: maxActive })}
          </span>
        </div>
      </div>

      {atLimit && (
        <p className="text-sm text-muted-foreground">{t('parent.children.limitReached', { max: maxActive })}</p>
      )}

      {active.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed p-10 text-center">
          <ChildAvatar avatarKey="owl" size="lg" />
          <h2 className="text-lg font-semibold">{t('parent.children.emptyTitle')}</h2>
          <p className="text-muted-foreground">{t('parent.children.emptyDescription')}</p>
        </div>
      ) : (
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {active.map((profile) => (
            <li
              key={profile.id}
              className="flex flex-col items-center gap-3 rounded-2xl border bg-card p-6 text-center"
            >
              <ChildAvatar avatarKey={profile.avatarKey} size="lg" />
              <h2 className="text-lg font-semibold break-all">{profile.nickname}</h2>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={() => setDialog({ open: true, profile })}>
                  <Pencil aria-hidden />
                  {t('parent.children.edit')}
                </Button>
                <ArchiveButton profile={profile} />
              </div>
            </li>
          ))}
        </ul>
      )}

      {archived.length > 0 && (
        <section aria-labelledby="archived-title" className="flex flex-col gap-3">
          <h2 id="archived-title" className="font-semibold text-muted-foreground">
            {t('parent.children.archivedTitle')}
          </h2>
          <ul className="flex flex-col gap-2">
            {archived.map((profile) => (
              <ArchivedRow key={profile.id} profile={profile} maxActive={maxActive} />
            ))}
          </ul>
        </section>
      )}

      <ChildProfileDialog
        open={dialog.open}
        profile={dialog.profile}
        onOpenChange={(open) => setDialog((current) => ({ ...current, open }))}
        avatarKeys={avatarKeys}
        maxActive={maxActive}
      />
    </section>
  )
}

function ArchiveButton({ profile }: { profile: ChildProfile }) {
  const { t } = useTranslation()
  const archive = useArchiveChildProfile()

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="ghost" size="sm" disabled={archive.isPending}>
          <Archive aria-hidden />
          {t('parent.children.archive')}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('parent.children.archiveTitle', { nickname: profile.nickname })}</AlertDialogTitle>
          <AlertDialogDescription>{t('parent.children.archiveDescription')}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t('parent.children.cancel')}</AlertDialogCancel>
          <AlertDialogAction onClick={() => archive.mutate(profile.id)}>
            {t('parent.children.archive')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function ArchivedRow({ profile, maxActive }: { profile: ChildProfile; maxActive: number }) {
  const { t } = useTranslation()
  const restore = useRestoreChildProfile()
  const error = restore.error instanceof ApiError ? restore.error.kind : restore.isError ? 'server' : null

  return (
    <li className="flex flex-wrap items-center gap-3 rounded-xl border p-3">
      <ChildAvatar avatarKey={profile.avatarKey} size="sm" className="opacity-60" />
      <span className="flex-1 font-medium break-all">{profile.nickname}</span>
      <Button variant="outline" size="sm" disabled={restore.isPending} onClick={() => restore.mutate(profile.id)}>
        <RotateCcw aria-hidden />
        {t('parent.children.restore')}
      </Button>
      {error && (
        <p role="alert" className="w-full text-sm text-destructive">
          {error === 'rule'
            ? t('parent.children.restoreLimit', { max: maxActive })
            : error === 'conflict'
              ? t('parent.children.restoreConflict')
              : t('parent.children.failed')}
        </p>
      )}
    </li>
  )
}
