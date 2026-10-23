import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { sessionStore } from '@/auth/session'
import { mockApi } from '@/test/api-mocks'
import { renderRoutes } from '@/test/render'
import { server } from '@/test/server'
import { signInAs } from '@/test/session'

function mainNav() {
  return screen.getByRole('navigation', { name: 'Điều hướng chính' })
}

describe('routes', () => {
  it('shows the admin shell with its navigation to a SuperAdmin', () => {
    signInAs('SuperAdmin')

    renderRoutes(routes, { route: '/admin/users' })

    expect(mainNav().querySelector('[aria-current="page"]')?.textContent).toBe('Người dùng')
    expect(screen.getByRole('heading', { name: 'Người dùng' })).toBeTruthy()
    expect(screen.getByText('superadmin@example.test')).toBeTruthy()
  })

  it('sends /parent to the children page', () => {
    signInAs('Parent')

    const { router } = renderRoutes(routes, { route: '/parent' })

    expect(router.state.location.pathname).toBe('/parent/children')
    expect(screen.getByRole('heading', { name: 'Con của tôi' })).toBeTruthy()
  })

  it('shows the play area in its own theme', () => {
    signInAs('Parent')

    renderRoutes(routes, { route: '/play' })

    expect(screen.getByRole('heading', { name: 'Cùng học tiếng Anh nào!' }).closest('.theme-play')).not.toBeNull()
  })

  it('shows the not-found page for an unknown address', async () => {
    server.use(
      mockApi('get', '/api/v1/foundation/status', () => ({ message: '', isFoundationExample: true, echo: null })),
    )
    const { user, router } = renderRoutes(routes, { route: '/khong-co-trang-nay' })

    expect(screen.getByRole('heading', { name: 'Không tìm thấy trang' })).toBeTruthy()

    await user.click(screen.getByRole('link', { name: 'Về trang chủ' }))
    expect(router.state.location.pathname).toBe('/')
  })
})

describe('route guards', () => {
  it.each(['/admin', '/admin/users', '/parent/progress', '/play'])(
    'send a signed-out visitor from %s to sign in',
    (path) => {
      const { router } = renderRoutes(routes, { route: path })

      expect(router.state.location.pathname).toBe('/login')
      expect(router.state.location.state).toEqual({ from: path })
    },
  )

  it('wait while the session is being restored', () => {
    sessionStore.set({ status: 'restoring' })

    const { router } = renderRoutes(routes, { route: '/admin' })

    expect(screen.getByRole('status').textContent).toBe('Đang kiểm tra phiên đăng nhập…')
    expect(router.state.location.pathname).toBe('/admin')
  })

  it('keep a Parent out of the admin area', () => {
    signInAs('Parent')

    renderRoutes(routes, { route: '/admin' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it('keep an Admin out of the parent area', () => {
    signInAs('SuperAdmin')

    renderRoutes(routes, { route: '/parent/children' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
  })

  it('show a ContentAdmin only the menu items they may use', () => {
    signInAs('ContentAdmin')

    renderRoutes(routes, { route: '/admin' })

    const links = within(mainNav())
      .getAllByRole('link')
      .map((link) => link.textContent)
    expect(links).toEqual(['Tổng quan', 'Nội dung'])
  })

  it('show the 403 page inside the layout for a page without permission', () => {
    signInAs('ContentAdmin')

    renderRoutes(routes, { route: '/admin/users' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
    expect(mainNav()).toBeTruthy()
  })
})

describe('sign out', () => {
  it('ends the session and opens the sign-in page', async () => {
    server.use(mockApi('post', '/api/v1/auth/logout', () => undefined))
    signInAs('SuperAdmin')
    const { user, router } = renderRoutes(routes, { route: '/admin' })

    await user.click(screen.getByRole('button', { name: 'Đăng xuất' }))

    expect(await screen.findByRole('heading', { name: 'Đăng nhập' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/login')
    expect(sessionStore.get().status).toBe('signed-out')
  })
})
