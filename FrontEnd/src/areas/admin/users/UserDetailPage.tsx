import { ArrowLeft } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router'
import { ApiError } from '@/shared/api/problem-details'
import { hasPermission, Permissions } from '@/shared/auth/permissions'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'
import { useUser } from '@/areas/admin/users/api'
import type { CreatedUserLocationState } from '@/areas/admin/users/CreateUserPage'
import { UserRoles, UserStatusBadges, useUserLabels } from '@/areas/admin/users/labels'
import { UserRolesEditor, UserStatusActions } from '@/areas/admin/users/UserManagement'
import { useDateTimeFormat } from '@/shared/lib/format'

export function UserDetailPage() {
  const { t } = useTranslation()
  const { userId = '' } = useParams()
  const user = useUser(userId)
  const labels = useUserLabels()
  const formatDateTime = useDateTimeFormat()
  const session = useSession()
  const justCreated = (useLocation().state as CreatedUserLocationState | null)?.created === true
  const canManage = session ? hasPermission(session.user, Permissions.usersManage) : false
  const canSetRoles = session ? hasPermission(session.user, Permissions.rolesManage) : false

  return (
    <section className="flex flex-col gap-6">
      <Button asChild variant="ghost" size="sm" className="self-start">
        <Link to="/admin/users">
          <ArrowLeft aria-hidden />
          {t('admin.users.detail.back')}
        </Link>
      </Button>

      {user.isPending ? (
        <p role="status" className="text-muted-foreground">
          {t('admin.users.loading')}
        </p>
      ) : user.isError ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          {user.error instanceof ApiError && user.error.kind === 'not-found' ? (
            <p>{t('admin.users.detail.notFound')}</p>
          ) : (
            <>
              <p>{t('admin.users.detail.loadError')}</p>
              <Button variant="outline" size="sm" onClick={() => void user.refetch()}>
                {t('admin.users.retry')}
              </Button>
            </>
          )}
        </div>
      ) : (
        <>
          {justCreated && (
            <p role="status" className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
              {user.data.userType === 'Admin'
                ? t('admin.users.create.createdAdmin')
                : t('admin.users.create.createdParent')}
            </p>
          )}
          <div className="flex flex-col gap-2">
            <h1 className="font-heading text-2xl font-semibold break-all">{user.data.email}</h1>
            <UserStatusBadges user={user.data} />
          </div>
          <dl className="grid gap-x-8 gap-y-4 sm:grid-cols-[max-content_1fr]">
            <Field label={t('admin.users.columns.userType')}>{labels.userType(user.data.userType)}</Field>
            <Field label={t('admin.users.columns.roles')}>
              <UserRoles roles={user.data.roles} />
            </Field>
            <Field label={t('admin.users.detail.createdAt')}>{formatDateTime(user.data.createdAtUtc)}</Field>
            <Field label={t('admin.users.detail.lastLogin')}>
              {user.data.lastLoginAtUtc ? formatDateTime(user.data.lastLoginAtUtc) : t('admin.users.never')}
            </Field>
            <Field label={t('admin.users.detail.userId')}>
              <code className="text-sm break-all">{user.data.id}</code>
            </Field>
          </dl>
          {canSetRoles && <UserRolesEditor user={user.data} isSelf={user.data.id === session?.user.userId} />}
          {canManage && <UserStatusActions user={user.data} isSelf={user.data.id === session?.user.userId} />}
        </>
      )}
    </section>
  )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="-mt-3 sm:mt-0">{children}</dd>
    </>
  )
}
