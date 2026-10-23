import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, Outlet, useLocation } from 'react-router'
import { ForbiddenPage } from '@/app/pages/ErrorPages'
import { hasPermission, type Permission, type UserType } from '@/auth/permissions'
import { useSessionState } from '@/auth/session'

/** State passed to /login so it can return to the page the user asked for. */
export interface LoginLocationState {
  from?: string
}

/** Shown while the session is being restored from the refresh cookie. */
export function RestoringSession() {
  const { t } = useTranslation()
  return (
    <main className="flex min-h-svh items-center justify-center p-4">
      <p role="status" className="text-muted-foreground">
        {t('auth.restoring')}
      </p>
    </main>
  )
}

/**
 * Renders the child routes only for a signed-in user, optionally of the given user types. Everyone else is sent to
 * sign in, or sees the 403 page. This is for navigation only: the API checks every request itself.
 */
export function RequireAuth({ userTypes }: { userTypes?: UserType[] }) {
  const state = useSessionState()
  const location = useLocation()

  if (state.status === 'restoring') {
    return <RestoringSession />
  }
  if (state.status === 'signed-out') {
    const from = `${location.pathname}${location.search}`
    return <Navigate to="/login" replace state={{ from } satisfies LoginLocationState} />
  }
  if (userTypes && !userTypes.some((userType) => userType === state.session.user.userType)) {
    return <ForbiddenPage />
  }
  return <Outlet />
}

/** Renders its children only when the signed-in user has the permission; otherwise the 403 page. */
export function RequirePermission({ permission, children }: { permission: Permission; children: ReactNode }) {
  const state = useSessionState()
  if (state.status !== 'signed-in' || !hasPermission(state.session.user, permission)) {
    return <ForbiddenPage embedded />
  }
  return children
}
