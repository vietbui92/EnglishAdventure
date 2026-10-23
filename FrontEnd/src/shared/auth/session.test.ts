import { HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { api, unwrap } from '@/shared/api/client'
import { refreshSession, restoreSession, sessionStore, signOut } from '@/shared/auth/session'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { server } from '@/shared/test/server'
import { signInAs, testUser } from '@/shared/test/session'

function tokens(accessToken: string) {
  return {
    accessToken,
    tokenType: 'Bearer',
    accessTokenExpiresAtUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
    refreshToken: null,
    refreshTokenExpiresAtUtc: new Date(Date.now() + 14 * 86_400_000).toISOString(),
  }
}

const emptyPage = { items: [], page: 1, pageSize: 20, totalCount: 0 }

describe('restoreSession', () => {
  it('signs in again from the refresh cookie as a web client', async () => {
    const refreshRequests: Request[] = []
    server.use(
      mockApi('post', '/api/v1/auth/refresh', ({ request }) => {
        refreshRequests.push(request.clone())
        return tokens('restored')
      }),
      mockApi('get', '/api/v1/auth/me', () => testUser('Parent')),
    )
    sessionStore.set({ status: 'restoring' })

    await restoreSession()

    expect(sessionStore.get()).toMatchObject({ status: 'signed-in', session: { accessToken: 'restored' } })
    const [refresh] = refreshRequests
    expect(refresh?.headers.get('X-EnglishAdventure-Client')).toBe('web')
    expect(await refresh?.text()).toBe('')
  })

  it('is signed out, without an expiry notice, when there is no valid cookie', async () => {
    server.use(mockApi('post', '/api/v1/auth/refresh', () => problem(401)))
    sessionStore.set({ status: 'restoring' })

    await restoreSession()

    expect(sessionStore.get()).toEqual({ status: 'signed-out', reason: undefined })
  })
})

describe('refreshSession', () => {
  it('sends one refresh for concurrent callers', async () => {
    let refreshes = 0
    server.use(
      mockApi('post', '/api/v1/auth/refresh', () => {
        refreshes += 1
        return tokens(`renewed-${refreshes}`)
      }),
      mockApi('get', '/api/v1/auth/me', () => testUser('SuperAdmin')),
    )
    signInAs('SuperAdmin')

    const results = await Promise.all([refreshSession(), refreshSession(), refreshSession()])

    expect(refreshes).toBe(1)
    expect(results.map((session) => session?.accessToken)).toEqual(['renewed-1', 'renewed-1', 'renewed-1'])
  })

  it('ends a signed-in session as expired when the cookie is rejected', async () => {
    server.use(mockApi('post', '/api/v1/auth/refresh', () => problem(401)))
    signInAs('SuperAdmin')

    expect(await refreshSession()).toBeNull()
    expect(sessionStore.get()).toEqual({ status: 'signed-out', reason: 'expired' })
  })
})

describe('a request rejected with 401', () => {
  it('is repeated once with a renewed token', async () => {
    const seenTokens: (string | null)[] = []
    server.use(
      mockApi('get', '/api/v1/admin/users', ({ request }) => {
        const authorization = request.headers.get('Authorization')
        seenTokens.push(authorization)
        return authorization === 'Bearer renewed' ? emptyPage : problem(401)
      }),
      mockApi('post', '/api/v1/auth/refresh', () => tokens('renewed')),
      mockApi('get', '/api/v1/auth/me', () => testUser('SuperAdmin')),
    )
    signInAs('SuperAdmin')

    const page = await unwrap(api.GET('/api/v1/admin/users'))

    expect(page.totalCount).toBe(0)
    expect(seenTokens).toEqual(['Bearer test-token-SuperAdmin', 'Bearer renewed'])
  })

  it('fails as unauthorized and signs out when the session cannot be renewed', async () => {
    server.use(
      mockApi('get', '/api/v1/admin/users', () => problem(401)),
      mockApi('post', '/api/v1/auth/refresh', () => problem(401)),
    )
    signInAs('SuperAdmin')

    await expect(unwrap(api.GET('/api/v1/admin/users'))).rejects.toMatchObject({ kind: 'unauthorized' })
    expect(sessionStore.get()).toEqual({ status: 'signed-out', reason: 'expired' })
  })

  it('shares one refresh between requests that failed together', async () => {
    let refreshes = 0
    server.use(
      mockApi('get', '/api/v1/admin/users', ({ request }) =>
        request.headers.get('Authorization') === 'Bearer renewed' ? emptyPage : problem(401),
      ),
      mockApi('post', '/api/v1/auth/refresh', () => {
        refreshes += 1
        return tokens('renewed')
      }),
      mockApi('get', '/api/v1/auth/me', () => testUser('SuperAdmin')),
    )
    signInAs('SuperAdmin')

    await Promise.all([1, 2, 3].map(() => unwrap(api.GET('/api/v1/admin/users'))))

    expect(refreshes).toBe(1)
  })
})

describe('signOut', () => {
  it('revokes the cookie session on the server and signs out', async () => {
    const logoutRequests: Request[] = []
    server.use(
      mockApi('post', '/api/v1/auth/logout', ({ request }) => {
        logoutRequests.push(request)
        return undefined
      }),
    )
    signInAs('Parent')

    await signOut()

    expect(logoutRequests[0]?.headers.get('X-EnglishAdventure-Client')).toBe('web')
    expect(sessionStore.get()).toEqual({ status: 'signed-out' })
  })

  it('signs out locally even when the server cannot be reached', async () => {
    server.use(mockApi('post', '/api/v1/auth/logout', () => HttpResponse.error()))
    signInAs('Parent')

    await signOut()

    expect(sessionStore.get().status).toBe('signed-out')
  })
})
