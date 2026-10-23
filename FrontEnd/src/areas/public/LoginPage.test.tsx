import { screen } from '@testing-library/react'
import { HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { sessionStore } from '@/shared/auth/session'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { signInAs, testUser } from '@/shared/test/session'
import { server } from '@/shared/test/server'

const tokens = {
  accessToken: 'access-token-1',
  tokenType: 'Bearer',
  accessTokenExpiresAtUtc: '2026-10-23T12:00:00Z',
  refreshToken: null,
  refreshTokenExpiresAtUtc: '2026-11-06T12:00:00Z',
}

async function renderLogin() {
  return renderRoutes(routes, { route: '/login' })
}

async function submit(user: Awaited<ReturnType<typeof renderLogin>>['user'], email: string, password: string) {
  if (email) await user.type(screen.getByLabelText('Email'), email)
  if (password) await user.type(screen.getByLabelText('Mật khẩu'), password)
  await user.click(screen.getByRole('button', { name: 'Đăng nhập' }))
}

describe('LoginPage', () => {
  it('signs an Admin in as a web client and opens the admin area', async () => {
    const requests: Request[] = []
    server.use(
      mockApi('post', '/api/v1/auth/login', async ({ request }) => {
        requests.push(request.clone())
        return tokens
      }),
      mockApi('get', '/api/v1/auth/me', ({ request }) => {
        requests.push(request)
        return testUser('SuperAdmin')
      }),
    )
    const { user, router } = await renderLogin()

    await submit(user, ' an@example.com ', 'Correct-horse-1')

    expect(await screen.findByRole('heading', { name: 'Tổng quan' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/admin')
    const [login, me] = requests
    expect(login?.headers.get('X-EnglishAdventure-Client')).toBe('web')
    expect(await login?.json()).toEqual({ email: 'an@example.com', password: 'Correct-horse-1' })
    expect(me?.headers.get('Authorization')).toBe('Bearer access-token-1')
    expect(sessionStore.get()).toMatchObject({ status: 'signed-in', session: { accessToken: 'access-token-1' } })
  })

  it('opens the parent area for a Parent', async () => {
    server.use(
      mockApi('post', '/api/v1/auth/login', () => tokens),
      mockApi('get', '/api/v1/auth/me', () => testUser('Parent')),
    )
    const { user, router } = await renderLogin()

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect(await screen.findByRole('heading', { name: 'Con của tôi' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/parent/children')
  })

  it('checks the fields before calling the API', async () => {
    const { user } = await renderLogin()

    await submit(user, '', '')
    expect(screen.getByText('Hãy nhập email.')).toBeTruthy()
    expect(screen.getByText('Hãy nhập mật khẩu.')).toBeTruthy()
    expect(screen.getByLabelText('Email').getAttribute('aria-invalid')).toBe('true')

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Đăng nhập' }))
    expect(screen.getByText('Email không hợp lệ.')).toBeTruthy()
  })

  it('reports wrong credentials without signing in', async () => {
    server.use(mockApi('post', '/api/v1/auth/login', () => problem(401, { title: 'Authentication failed.' })))
    const { user, router } = await renderLogin()

    await submit(user, 'an@example.com', 'Wrong-password-1')

    expect((await screen.findByRole('alert')).textContent).toContain('Email hoặc mật khẩu không đúng.')
    expect(router.state.location.pathname).toBe('/login')
    expect(sessionStore.get().status).toBe('signed-out')
  })

  it('reports a deactivated account', async () => {
    server.use(mockApi('post', '/api/v1/auth/login', () => problem(403, { title: 'Access denied.' })))
    const { user } = await renderLogin()

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect((await screen.findByRole('alert')).textContent).toContain('Tài khoản này đã bị vô hiệu hóa.')
  })

  it('marks the fields the server rejects', async () => {
    server.use(
      mockApi('post', '/api/v1/auth/login', () =>
        problem(400, { title: 'Validation failed.', errors: { Email: ['Email is not valid.'] } }),
      ),
    )
    const { user } = await renderLogin()

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect(await screen.findByText('Giá trị này chưa hợp lệ.')).toBeTruthy()
    expect(screen.getByLabelText('Email').getAttribute('aria-invalid')).toBe('true')
  })

  it('reports an unreachable server', async () => {
    server.use(mockApi('post', '/api/v1/auth/login', () => HttpResponse.error()))
    const { user } = await renderLogin()

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect((await screen.findByRole('alert')).textContent).toContain('Không kết nối được máy chủ.')
  })

  it('sends a signed-in user to their area', async () => {
    signInAs('Parent')

    const { router } = await renderLogin()

    expect(router.state.location.pathname).toBe('/parent/children')
  })

  it('returns to the page that asked for sign-in', async () => {
    server.use(
      mockApi('post', '/api/v1/auth/login', () => tokens),
      mockApi('get', '/api/v1/auth/me', () => testUser('SuperAdmin')),
    )
    const { user, router } = await renderRoutes(routes, { route: '/admin/roles' })
    expect(router.state.location.pathname).toBe('/login')

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect(await screen.findByRole('heading', { name: 'Vai trò' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/admin/roles')
  })

  it('ignores a return page outside the user area', async () => {
    server.use(
      mockApi('post', '/api/v1/auth/login', () => tokens),
      mockApi('get', '/api/v1/auth/me', () => testUser('Parent')),
    )
    const { user, router } = await renderRoutes(routes, { route: '/admin/users' })

    await submit(user, 'an@example.com', 'Correct-horse-1')

    expect(await screen.findByRole('heading', { name: 'Con của tôi' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/parent/children')
  })

  it('explains that an expired session needs a new sign-in', async () => {
    sessionStore.set({ status: 'signed-out', reason: 'expired' })

    await renderLogin()

    expect(screen.getByRole('alert').textContent).toContain('Phiên đăng nhập đã hết hạn.')
  })
})
