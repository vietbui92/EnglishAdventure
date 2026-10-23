import createClient, { type Middleware } from 'openapi-fetch'
import { ApiError, toApiError } from '@/shared/api/problem-details'
import type { paths } from '@/shared/api/schema'

/**
 * Sent on login, refresh and logout so the API keeps the refresh token in its httpOnly cookie
 * (see BackEnd RefreshTokenCookie). The cookie is same-origin, so fetch sends it without extra options.
 */
export const webClientHeaders = { 'X-EnglishAdventure-Client': 'web' } as const

let getAccessToken: () => string | null = () => null
let renewAccessToken: ((rejectedToken: string | null) => Promise<string | null>) | null = null

/** Called by the session. The token stays in memory only. */
export function setAccessTokenProvider(provider: () => string | null) {
  getAccessToken = provider
}

/**
 * Called by the session. On a 401 the client asks for a new access token once (the session refreshes it through the
 * cookie) and repeats the request with it; `null` means the session has ended. It receives the token the server rejected,
 * so requests that failed together share one refresh.
 */
export function setAccessTokenRenewer(renewer: ((rejectedToken: string | null) => Promise<string | null>) | null) {
  renewAccessToken = renewer
}

// A request body can be read only once, so a copy is kept until the response shows whether a retry is needed.
const pendingCopies = new Map<string, Request>()

const bearerToken: Middleware = {
  onRequest({ request, schemaPath, id }) {
    const token = getAccessToken()
    // The sign-in flow sets its own Authorization header for /me before the session exists.
    if (token && !request.headers.has('Authorization')) {
      request.headers.set('Authorization', `Bearer ${token}`)
    }
    if (!isAuthEndpoint(schemaPath)) {
      pendingCopies.set(id, request.clone())
    }
    return request
  },
  async onResponse({ response, id }) {
    const copy = pendingCopies.get(id)
    pendingCopies.delete(id)
    if (response.status !== 401 || !copy || !renewAccessToken) {
      return response
    }

    const rejectedToken = copy.headers.get('Authorization')?.replace(/^Bearer /, '') ?? null
    const token = await renewAccessToken(rejectedToken)
    if (!token) {
      return response
    }
    copy.headers.set('Authorization', `Bearer ${token}`)
    return globalThis.fetch(copy)
  },
  onError({ id }) {
    pendingCopies.delete(id)
  },
}

// The auth endpoints answer 401 for their own reasons, and /me runs inside the refresh itself; retrying would loop.
function isAuthEndpoint(schemaPath: string) {
  return schemaPath.startsWith('/api/v1/auth/')
}

// Same origin as the page: the Vite dev server (and later the production host) forwards /api to the backend.
// fetch is looked up per request, so test mocks (MSW) that replace it after this module loads still apply.
export const api = createClient<paths>({
  baseUrl: window.location.origin,
  fetch: (request) => globalThis.fetch(request),
})
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
