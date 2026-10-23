import { useSyncExternalStore } from 'react'
import { api, setAccessTokenProvider, setAccessTokenRenewer, unwrap, webClientHeaders } from '@/shared/api/client'
import { ApiError } from '@/shared/api/problem-details'
import type { components } from '@/shared/api/schema'

export type CurrentUser = components['schemas']['CurrentUserResponse']
type AuthTokens = components['schemas']['AuthTokensResponse']

export interface Session {
  accessToken: string
  accessTokenExpiresAtUtc: string
  user: CurrentUser
}

export type SessionState =
  { status: 'restoring' } | { status: 'signed-out'; reason?: 'expired' } | { status: 'signed-in'; session: Session }

// Refresh this long before the access token expires, so requests rarely meet a 401.
const refreshLeadMs = 60_000
const minRefreshDelayMs = 5_000

// In memory only. On page load the session is restored from the refresh cookie (restoreSession).
let state: SessionState = { status: 'restoring' }
let refreshTimer: ReturnType<typeof setTimeout> | undefined
let refreshInFlight: Promise<Session | null> | null = null
const listeners = new Set<() => void>()

// Tabs share the refresh cookie: one tab signing out ends the others, one signing in lets the others restore.
const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('english-adventure-session')
channel?.addEventListener('message', (event: MessageEvent<'signed-in' | 'signed-out'>) => {
  if (event.data === 'signed-out' && state.status === 'signed-in') {
    setState({ status: 'signed-out' })
  } else if (event.data === 'signed-in' && state.status !== 'signed-in') {
    void restoreSession()
  }
})

function setState(next: SessionState) {
  clearTimeout(refreshTimer)
  state = next
  if (next.status === 'signed-in') {
    const expiresAt = Date.parse(next.session.accessTokenExpiresAtUtc)
    const delay = Math.max(expiresAt - Date.now() - refreshLeadMs, minRefreshDelayMs)
    refreshTimer = setTimeout(() => void refreshSession(), delay)
  }
  listeners.forEach((listener) => listener())
}

export const sessionStore = {
  get: () => state,
  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
  /** For tests: set the state directly, without calling the API. */
  set: setState,
}

/** The session state. Re-renders when it changes. */
export function useSessionState() {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.get)
}

/** The signed-in session, or null while restoring or signed out. */
export function useSession() {
  const current = useSessionState()
  return current.status === 'signed-in' ? current.session : null
}

/** Loads the profile for new tokens and makes them the session. */
async function startSession(tokens: AuthTokens): Promise<Session> {
  const user = await unwrap(api.GET('/api/v1/auth/me', { headers: { Authorization: `Bearer ${tokens.accessToken}` } }))
  const session = { accessToken: tokens.accessToken, accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc, user }
  setState({ status: 'signed-in', session })
  return session
}

export async function signInWithPassword(email: string, password: string): Promise<Session> {
  const tokens = await unwrap(api.POST('/api/v1/auth/login', { body: { email, password }, headers: webClientHeaders }))
  const session = await startSession(tokens)
  channel?.postMessage('signed-in')
  return session
}

// Tabs must not refresh at the same moment: the API treats a second use of the same refresh token as theft and ends
// the whole session. A Web Lock makes the tabs take turns; each then sends the cookie the previous one received.
function withRefreshLock<T>(task: () => Promise<T>): Promise<T> {
  const locks = typeof navigator === 'undefined' ? undefined : navigator.locks
  return locks ? locks.request('english-adventure-refresh', task) : task()
}

/**
 * Exchanges the refresh cookie for new tokens and reloads the profile (so permission changes show up).
 * Concurrent callers share one request. Resolves to null, and signs out, when the session cannot be renewed.
 */
export function refreshSession(): Promise<Session | null> {
  refreshInFlight ??= withRefreshLock(async () => {
    try {
      const tokens = await unwrap(api.POST('/api/v1/auth/refresh', { headers: webClientHeaders }))
      return await startSession(tokens)
    } catch (error) {
      const wasSignedIn = state.status === 'signed-in'
      // A network failure is not proof the session ended, but without a valid token nothing works either.
      setState({ status: 'signed-out', reason: wasSignedIn && isAuthFailure(error) ? 'expired' : undefined })
      return null
    }
  }).finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

function isAuthFailure(error: unknown) {
  return error instanceof ApiError && (error.kind === 'unauthorized' || error.kind === 'forbidden')
}

/** On page load: signed in again if the refresh cookie is still valid, otherwise signed out. */
export async function restoreSession(): Promise<void> {
  await refreshSession()
}

/** Ends the session on the server (revokes the refresh cookie) and in every tab. */
export async function signOut(): Promise<void> {
  try {
    await unwrap(api.POST('/api/v1/auth/logout', { headers: webClientHeaders }))
  } catch {
    // Signed out locally anyway; the refresh token expires on its own.
  }
  setState({ status: 'signed-out' })
  channel?.postMessage('signed-out')
}

setAccessTokenProvider(() => (state.status === 'signed-in' ? state.session.accessToken : null))
setAccessTokenRenewer(async (rejectedToken) => {
  if (state.status !== 'signed-in') {
    return null
  }
  // Another request already renewed the token this one was sent with.
  if (state.session.accessToken !== rejectedToken) {
    return state.session.accessToken
  }
  const renewed = await refreshSession()
  return renewed?.accessToken ?? null
})
