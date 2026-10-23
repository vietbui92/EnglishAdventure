import type { ParseKeys } from 'i18next'
import { CircleAlert, TriangleAlert } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
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
import { Button } from '@/shared/components/ui/button'
import { Checkbox } from '@/shared/components/ui/checkbox'
import { Label } from '@/shared/components/ui/label'
import { useSetUserActive, useSetUserRoles, type UserSummary } from '@/areas/admin/users/api'
import { useUserLabels } from '@/areas/admin/users/labels'

/** Roles an Admin user can be given until role management (S1.5) lists the real roles. */
const assignableAdminRoles = ['SuperAdmin', 'ContentAdmin']

const roleDescriptionKeys: Record<string, ParseKeys> = {
  SuperAdmin: 'admin.users.rolesEditor.roleDescriptions.SuperAdmin',
  ContentAdmin: 'admin.users.rolesEditor.roleDescriptions.ContentAdmin',
}

function ErrorMessage({ messageKey }: { messageKey: ParseKeys }) {
  const { t } = useTranslation()
  return (
    <Alert variant="destructive">
      <CircleAlert aria-hidden />
      <AlertDescription>{t(messageKey)}</AlertDescription>
    </Alert>
  )
}

// The client never offers self-changes, so a 422 from these endpoints can only be the last-SuperAdmin rule.
function isRuleViolation(error: unknown) {
  return error instanceof ApiError && error.kind === 'rule'
}

/** Deactivate (with confirmation) or reactivate the account. Needs users.manage. */
export function UserStatusActions({ user, isSelf }: { user: UserSummary; isSelf: boolean }) {
  const { t } = useTranslation()
  const setActive = useSetUserActive(user.id)
  const [done, setDone] = useState<'activated' | 'deactivated' | null>(null)

  const run = (active: boolean) => {
    setDone(null)
    setActive.mutate(active, { onSuccess: () => setDone(active ? 'activated' : 'deactivated') })
  }

  return (
    <section aria-labelledby="status-title" className="flex flex-col gap-3">
      <h2 id="status-title" className="text-lg font-semibold">
        {t('admin.users.actions.title')}
      </h2>

      {isSelf ? (
        <p className="text-sm text-muted-foreground">{t('admin.users.actions.self')}</p>
      ) : user.isActive ? (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="destructive" className="self-start" disabled={setActive.isPending}>
              {t('admin.users.actions.deactivate')}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('admin.users.actions.confirmTitle', { email: user.email })}</AlertDialogTitle>
              <AlertDialogDescription>{t('admin.users.actions.confirmDescription')}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t('admin.users.actions.cancel')}</AlertDialogCancel>
              <AlertDialogAction variant="destructive" onClick={() => run(false)}>
                {t('admin.users.actions.confirm')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      ) : (
        <Button variant="outline" className="self-start" disabled={setActive.isPending} onClick={() => run(true)}>
          {setActive.isPending ? t('admin.users.actions.activating') : t('admin.users.actions.activate')}
        </Button>
      )}

      {setActive.isError && (
        <ErrorMessage
          messageKey={
            isRuleViolation(setActive.error) ? 'admin.users.actions.lastSuperAdmin' : 'admin.users.actions.failed'
          }
        />
      )}
      {done && (
        <p role="status" className="text-sm text-muted-foreground">
          {t(`admin.users.actions.${done}`)}
        </p>
      )}
    </section>
  )
}

/** Choose an Admin user's roles. Needs roles.manage. A Parent user always keeps the Parent role. */
export function UserRolesEditor({ user, isSelf }: { user: UserSummary; isSelf: boolean }) {
  const { t } = useTranslation()

  return (
    <section aria-labelledby="roles-title" className="flex flex-col gap-3">
      <h2 id="roles-title" className="text-lg font-semibold">
        {t('admin.users.columns.roles')}
      </h2>
      {user.userType !== 'Admin' ? (
        <p className="text-sm text-muted-foreground">{t('admin.users.rolesEditor.parentOnly')}</p>
      ) : isSelf ? (
        <p className="text-sm text-muted-foreground">{t('admin.users.actions.self')}</p>
      ) : (
        <RolesForm user={user} />
      )}
    </section>
  )
}

function RolesForm({ user }: { user: UserSummary }) {
  const { t } = useTranslation()
  const labels = useUserLabels()
  const setRoles = useSetUserRoles(user.id)
  const [selected, setSelected] = useState(() => new Set(user.roles))
  const [saved, setSaved] = useState(false)

  // Roles the user already has but this list does not know (created later in S1.5) stay selectable, so a save keeps them.
  const options = [...new Set([...assignableAdminRoles, ...user.roles])]
  const changed = selected.size !== user.roles.length || user.roles.some((role) => !selected.has(role))

  const toggle = (role: string, checked: boolean) => {
    setSaved(false)
    setSelected((current) => {
      const next = new Set(current)
      if (checked) next.add(role)
      else next.delete(role)
      return next
    })
  }

  const save = () => {
    setSaved(false)
    setRoles.mutate(
      options.filter((role) => selected.has(role)),
      { onSuccess: () => setSaved(true) },
    )
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">{t('admin.users.rolesEditor.description')}</p>
      <div className="flex flex-col gap-3">
        {options.map((role) => {
          const descriptionKey = roleDescriptionKeys[role]
          return (
            <div key={role} className="flex items-start gap-3">
              <Checkbox
                id={`role-${role}`}
                checked={selected.has(role)}
                onCheckedChange={(checked) => toggle(role, checked === true)}
                aria-describedby={descriptionKey ? `role-${role}-hint` : undefined}
              />
              <div className="grid gap-0.5">
                <Label htmlFor={`role-${role}`}>{labels.role(role)}</Label>
                {descriptionKey && (
                  <span id={`role-${role}-hint`} className="text-sm text-muted-foreground">
                    {t(descriptionKey)}
                  </span>
                )}
              </div>
            </div>
          )
        })}
      </div>

      {selected.size === 0 && (
        <Alert>
          <TriangleAlert aria-hidden />
          <AlertDescription>{t('admin.users.rolesEditor.noRolesWarning')}</AlertDescription>
        </Alert>
      )}
      {setRoles.isError && (
        <ErrorMessage
          messageKey={
            isRuleViolation(setRoles.error)
              ? 'admin.users.rolesEditor.lastSuperAdmin'
              : 'admin.users.rolesEditor.failed'
          }
        />
      )}

      <div className="flex items-center gap-3">
        <Button disabled={!changed || setRoles.isPending} onClick={save}>
          {setRoles.isPending ? t('admin.users.rolesEditor.saving') : t('admin.users.rolesEditor.save')}
        </Button>
        {saved && (
          <span role="status" className="text-sm text-muted-foreground">
            {t('admin.users.rolesEditor.saved')}
          </span>
        )}
      </div>
    </div>
  )
}
