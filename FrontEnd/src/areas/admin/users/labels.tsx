import type { ParseKeys } from 'i18next'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/shared/components/ui/badge'
import type { UserSummary } from '@/areas/admin/users/api'

const userTypeKeys: Record<string, ParseKeys> = {
  Admin: 'admin.users.userType.Admin',
  Parent: 'admin.users.userType.Parent',
  Child: 'admin.users.userType.Child',
}

// The three system roles have translated names; roles created at runtime (S1.5) show their own name.
const roleKeys: Record<string, ParseKeys> = {
  SuperAdmin: 'admin.users.roles.SuperAdmin',
  ContentAdmin: 'admin.users.roles.ContentAdmin',
  Parent: 'admin.users.roles.Parent',
}

export function useUserLabels() {
  const { t } = useTranslation()
  return {
    userType: (userType: string) => {
      const key = userTypeKeys[userType]
      return key ? t(key) : userType
    },
    role: (role: string) => {
      const key = roleKeys[role]
      return key ? t(key) : role
    },
  }
}

export function UserStatusBadges({ user }: { user: Pick<UserSummary, 'isActive' | 'isLockedOut'> }) {
  const { t } = useTranslation()
  return (
    <span className="flex flex-wrap gap-1">
      {user.isActive ? (
        <Badge variant="secondary">{t('admin.users.status.active')}</Badge>
      ) : (
        <Badge variant="destructive">{t('admin.users.status.inactive')}</Badge>
      )}
      {user.isLockedOut && <Badge variant="outline">{t('admin.users.status.locked')}</Badge>}
    </span>
  )
}

export function UserRoles({ roles }: { roles: string[] }) {
  const { t } = useTranslation()
  const labels = useUserLabels()
  if (roles.length === 0) {
    return <span className="text-muted-foreground">{t('admin.users.noRoles')}</span>
  }
  return (
    <span className="flex flex-wrap gap-1">
      {roles.map((role) => (
        <Badge key={role} variant="outline">
          {labels.role(role)}
        </Badge>
      ))}
    </span>
  )
}
