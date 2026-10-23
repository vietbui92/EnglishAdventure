import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { sessionStore } from '@/shared/auth/session'
import { mockApi } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

function mainNav() {
  return screen.getByRole('navigation', { name: 'Điều hướng chính' })
}

describe('routes', () => {
  it('shows the admin shell with its navigation to a SuperAdmin', async () => {
    server.use(mockApi('get', '/api/v1/admin/users', () => ({ items: [], page: 1, pageSize: 20, totalCount: 0 })))
    signInAs('SuperAdmin')

    await renderRoutes(routes, { route: '/admin/users' })

    expect(mainNav().querySelector('[aria-current="page"]')?.textContent).toBe('Người dùng')
    expect(screen.getByRole('heading', { name: 'Người dùng' })).toBeTruthy()
    expect(await screen.findByText('Chưa có người dùng nào.')).toBeTruthy()
    expect(screen.getByText('superadmin@example.test')).toBeTruthy()
  })

  it('sends /parent to the children page', async () => {
    signInAs('Parent')

    const { router } = await renderRoutes(routes, { route: '/parent' })

    expect(router.state.location.pathname).toBe('/parent/children')
    expect(screen.getByRole('heading', { name: 'Con của tôi' })).toBeTruthy()
  })

  it('shows the kids area in its own theme', async () => {
    signInAs('Parent')

    await renderRoutes(routes, { route: '/kids' })

    expect(screen.getByRole('heading', { name: 'Cùng học tiếng Anh nào!' }).closest('.theme-kids')).not.toBeNull()
  })

  it('shows the not-found page for an unknown address', async () => {
    const { user, router } = await renderRoutes(routes, { route: '/khong-co-trang-nay' })

    expect(screen.getByRole('heading', { name: 'Không tìm thấy trang' })).toBeTruthy()

    await user.click(screen.getByRole('link', { name: 'Về trang chủ' }))
    expect(router.state.location.pathname).toBe('/')
  })
})

describe('route guards', () => {
  it.each(['/admin', '/admin/users', '/parent/progress', '/kids'])(
    'send a signed-out visitor from %s to sign in',
    async (path) => {
      const { router } = await renderRoutes(routes, { route: path })

      expect(router.state.location.pathname).toBe('/login')
      expect(router.state.location.state).toEqual({ from: path })
    },
  )

  it('wait while the session is being restored', async () => {
    sessionStore.set({ status: 'restoring' })

    const { router } = await renderRoutes(routes, { route: '/admin' })

    expect(screen.getByRole('status').textContent).toBe('Đang kiểm tra phiên đăng nhập…')
    expect(router.state.location.pathname).toBe('/admin')
  })

  it('keep a Parent out of the admin area', async () => {
    signInAs('Parent')

    await renderRoutes(routes, { route: '/admin' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it('keep an Admin out of the parent area', async () => {
    signInAs('SuperAdmin')

    await renderRoutes(routes, { route: '/parent/children' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
  })

  it('show a ContentAdmin only the menu items they may use', async () => {
    signInAs('ContentAdmin')

    await renderRoutes(routes, { route: '/admin' })

    const links = within(mainNav())
      .getAllByRole('link')
      .map((link) => link.textContent)
    expect(links).toEqual(['Tổng quan', 'Nội dung'])
  })

  it('show the 403 page inside the layout for a page without permission', async () => {
    signInAs('ContentAdmin')

    await renderRoutes(routes, { route: '/admin/users' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
    expect(mainNav()).toBeTruthy()
  })
})

describe('sign out', () => {
  it('ends the session and opens the sign-in page', async () => {
    server.use(mockApi('post', '/api/v1/auth/logout', () => undefined))
    signInAs('SuperAdmin')
    const { user, router } = await renderRoutes(routes, { route: '/admin' })

    await user.click(screen.getByRole('button', { name: 'Đăng xuất' }))

    expect(await screen.findByRole('heading', { name: 'Đăng nhập' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/login')
    expect(sessionStore.get().status).toBe('signed-out')
  })
})
