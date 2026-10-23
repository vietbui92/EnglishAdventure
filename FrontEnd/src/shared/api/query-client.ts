import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/problem-details'
import { sessionStore } from '@/shared/auth/session'

// Only failures that may pass on their own are retried; a 4xx answer will not change by asking again.
function shouldRetry(failureCount: number, error: unknown) {
  if (error instanceof ApiError && error.kind !== 'network' && error.kind !== 'server') {
    return false
  }
  return failureCount < 2
}

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: shouldRetry, staleTime: 30_000, refetchOnWindowFocus: false },
      mutations: { retry: false },
    },
  })
}

/**
 * Drops all cached server data whenever the session ends (sign-out, expiry, or another tab signing out), so the next
 * user of this browser never sees the previous user's data. Returns the unsubscribe function.
 */
export function clearCacheOnSignOut(client: QueryClient) {
  return sessionStore.subscribe(() => {
    if (sessionStore.get().status === 'signed-out') {
      client.clear()
    }
  })
}
