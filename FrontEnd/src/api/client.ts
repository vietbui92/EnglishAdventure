import createClient, { type Middleware } from 'openapi-fetch'
import { ApiError, toApiError } from '@/api/problem-details'
import type { paths } from '@/api/schema'

/**
 * Sent on login, refresh and logout so the API keeps the refresh token in its httpOnly cookie
 * (see BackEnd RefreshTokenCookie). The cookie is same-origin, so fetch sends it without extra options.
 */
export const webClientHeaders = { 'X-EnglishAdventure-Client': 'web' } as const

let getAccessToken: () => string | null = () => null

/** Called by the session (F1.2). The token stays in memory only. */
export function setAccessTokenProvider(provider: () => string | null) {
  getAccessToken = provider
}

const bearerToken: Middleware = {
  onRequest({ request }) {
    const token = getAccessToken()
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`)
    }
    return request
  },
}

// Same origin as the page: the Vite dev server (and later the production host) forwards /api to the backend.
export const api = createClient<paths>({ baseUrl: window.location.origin })
api.use(bearerToken)

type FetchResult<T> = { data?: T; error?: unknown; response: Response }

/** Returns the response data, or throws an {@link ApiError} for a failed or unreachable request. */
export async function unwrap<T>(request: Promise<FetchResult<T>>): Promise<T> {
  let result: FetchResult<T>
  try {
    result = await request
  } catch (cause) {
    throw new ApiError('network', 0, null, { cause })
  }

  if (!result.response.ok) {
    throw toApiError(result.response.status, result.error)
  }
  return result.data as T
}
