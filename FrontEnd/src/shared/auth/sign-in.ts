import { isChildSession, signInWithPassword, type CurrentUser, type Session } from '@/shared/auth/session'

export interface Credentials {
  email: string
  password: string
}

/**
 * Signs in, loads the user's profile and permissions, and stores the session. The refresh token stays in the httpOnly
 * cookie the API sets; only the access token is kept, in memory.
 */
export function signIn({ email, password }: Credentials): Promise<Session> {
  return signInWithPassword(email, password)
}

/** Where a session lands: a child session in the kids area, a user in their own area. */
export function homePathFor(user: CurrentUser): string {
  if (isChildSession(user)) {
    return '/kids'
  }
  switch (user.userType) {
    case 'Admin':
      return '/admin'
    case 'Parent':
      return '/parent'
    default:
      return '/'
  }
}

/**
 * The page to open after signing in: the page that sent the user to sign in, when it belongs to their own area,
 * otherwise their area's home. Only same-app paths are accepted, never a full URL.
 */
export function pathAfterSignIn(user: CurrentUser, from: string | undefined): string {
  const home = homePathFor(user)
  if (
    from &&
    from.startsWith('/') &&
    !from.startsWith('//') &&
    home !== '/' &&
    (from === home || from.startsWith(`${home}/`))
  ) {
    return from
  }
  return home
}
