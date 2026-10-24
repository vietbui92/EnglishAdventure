import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { sessionStore } from '@/shared/auth/session'
import { mockApi, mockEmptyAdventure, mockFamilyWithoutChildren, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs, signInAsChild, testUser } from '@/shared/test/session'

const bin = {
  id: '00000000-0000-0000-0000-0000000000b1',
  nickname: 'Bin',
  avatarKey: 'owl',
  status: 'Active',
  createdAtUtc: '2026-10-24T08:00:00Z',
  archivedAtUtc: null,
}

const childTokens = {
  accessToken: 'child-access',
  tokenType: 'Bearer',
  accessTokenExpiresAtUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
  refreshToken: null,
  refreshTokenExpiresAtUtc: new Date(Date.now() + 14 * 86_400_000).toISOString(),
}

describe('child mode', () => {
  // Child mode opens the map.
  beforeEach(() => server.use(...mockEmptyAdventure()))

  it('lets a parent hand the device to a child after confirming', async () => {
    const requests: Request[] = []
    server.use(
      mockFamilyWithoutChildren()[1]!,
      mockApi('get', '/api/v1/family/children', () => [bin]),
      mockApi('post', '/api/v1/auth/child-session', ({ request }) => {
        requests.push(request.clone())
        return childTokens
      }),
      mockApi('get', '/api/v1/auth/me', ({ request }) => {
        requests.push(request)
        return testUser('Parent', {
          roles: [],
          permissions: ['learning.play'],
          session: 'child',
          childProfile: { id: bin.id, nickname: 'Bin', avatarKey: 'owl' },
        })
      }),
    )
    signInAs('Parent')
    const { user, router } = await renderRoutes(routes, { route: '/parent/children' })

    await user.click(await screen.findByRole('button', { name: 'Cho con học' }))
    const confirm = within(await screen.findByRole('alertdialog'))
    expect(confirm.getByText('Cho Bin học trên thiết bị này?')).toBeTruthy()
    expect(confirm.getByText(/bạn cần đăng nhập lại/)).toBeTruthy()
    await user.click(confirm.getByRole('button', { name: 'Bắt đầu học' }))

    expect(await screen.findByRole('heading', { name: 'Xin chào, Bin!' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/kids')
    const [start, me] = requests
    expect(start?.headers.get('X-EnglishAdventure-Client')).toBe('web')
    expect(await start?.json()).toEqual({ childProfileId: bin.id, refreshToken: null })
    expect(me?.headers.get('Authorization')).toBe('Bearer child-access')
    expect(sessionStore.get()).toMatchObject({ status: 'signed-in', session: { user: { session: 'child' } } })
  })

  it('stays on the parent page when the confirmation is cancelled', async () => {
    let started = false
    server.use(
      mockFamilyWithoutChildren()[1]!,
      mockApi('get', '/api/v1/family/children', () => [bin]),
      mockApi('post', '/api/v1/auth/child-session', () => {
        started = true
        return childTokens
      }),
    )
    signInAs('Parent')
    const { user, router } = await renderRoutes(routes, { route: '/parent/children' })

    await user.click(await screen.findByRole('button', { name: 'Cho con học' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Hủy' }))

    expect(started).toBe(false)
    expect(router.state.location.pathname).toBe('/parent/children')
  })

  it('reports a child mode that could not start', async () => {
    server.use(
      mockFamilyWithoutChildren()[1]!,
      mockApi('get', '/api/v1/family/children', () => [bin]),
      mockApi('post', '/api/v1/auth/child-session', () => problem(422)),
    )
    signInAs('Parent')
    const { user } = await renderRoutes(routes, { route: '/parent/children' })

    await user.click(await screen.findByRole('button', { name: 'Cho con học' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Bắt đầu học' }))

    expect((await screen.findByRole('alert')).textContent).toContain('Không chuyển được sang chế độ học.')
  })

  it('leaves child mode only after confirmation, and then asks for a sign-in', async () => {
    const logouts: Request[] = []
    server.use(
      mockApi('post', '/api/v1/auth/logout', ({ request }) => {
        logouts.push(request)
        return undefined
      }),
    )
    signInAsChild({ nickname: 'Bin' })
    const { user, router } = await renderRoutes(routes, { route: '/kids' })

    await user.click(screen.getByRole('button', { name: 'Thoát' }))
    const confirm = within(await screen.findByRole('alertdialog'))
    expect(confirm.getByText('Bố mẹ sẽ cần đăng nhập lại để quay về trang phụ huynh.')).toBeTruthy()
    await user.click(confirm.getByRole('button', { name: 'Ở lại học tiếp' }))
    expect(router.state.location.pathname).toBe('/kids')
    expect(logouts).toEqual([])

    await user.click(screen.getByRole('button', { name: 'Thoát' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Thoát' }))

    expect(await screen.findByRole('heading', { name: 'Đăng nhập' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/login')
    expect(logouts[0]?.headers.get('X-EnglishAdventure-Client')).toBe('web')
    expect(sessionStore.get().status).toBe('signed-out')
  })
})
