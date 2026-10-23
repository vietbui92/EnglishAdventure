import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { renderRoutes } from '@/shared/test/render'
import { signInAs } from '@/shared/test/session'

describe('LandingPage', () => {
  it('introduces the product and invites parents to register or sign in', async () => {
    await renderRoutes(routes, { route: '/' })

    expect(screen.getByRole('heading', { level: 1, name: 'Học tiếng Anh như một chuyến phiêu lưu' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Mỗi ngày một chặng đường' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Bắt đầu trong ba bước' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'An tâm cho phụ huynh' })).toBeTruthy()

    const header = within(screen.getByRole('banner'))
    expect(header.getByRole('link', { name: 'Đăng nhập' }).getAttribute('href')).toBe('/login')
    expect(header.getByRole('link', { name: 'Đăng ký' }).getAttribute('href')).toBe('/register')
    expect(screen.getByRole('link', { name: 'Đăng ký cho phụ huynh' }).getAttribute('href')).toBe('/register')
  })

  it('links the sections from the header', async () => {
    await renderRoutes(routes, { route: '/' })

    const nav = within(screen.getByRole('navigation', { name: 'Điều hướng chính' }))
    expect(nav.getByRole('link', { name: 'Tính năng' }).getAttribute('href')).toBe('#features')
    expect(document.getElementById('features')).not.toBeNull()
    expect(document.getElementById('how-it-works')).not.toBeNull()
    expect(document.getElementById('parents')).not.toBeNull()
  })

  it('leads a signed-in user to their own area instead', async () => {
    signInAs('Parent')

    await renderRoutes(routes, { route: '/' })

    expect(screen.getByRole('link', { name: 'Vào trang của tôi' }).getAttribute('href')).toBe('/parent')
    expect(screen.queryByRole('link', { name: 'Đăng ký' })).toBeNull()
    expect(screen.queryByRole('link', { name: 'Đăng ký ngay' })).toBeNull()
  })

  it('switches to English', async () => {
    const { user } = await renderRoutes(routes, { route: '/' })

    await user.click(screen.getByRole('button', { name: 'English' }))

    expect(screen.getByRole('heading', { level: 1, name: 'Learning English is an adventure' })).toBeTruthy()
    expect(document.documentElement.lang).toBe('en')
  })
})

describe('RegisterPage', () => {
  it('explains that online registration opens soon and links to sign-in', async () => {
    const { user, router } = await renderRoutes(routes, { route: '/' })

    await user.click(within(screen.getByRole('banner')).getByRole('link', { name: 'Đăng ký' }))

    expect(await screen.findByRole('heading', { name: 'Đăng ký tài khoản phụ huynh' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/register')

    await user.click(within(screen.getByRole('main')).getByRole('link', { name: 'Đăng nhập' }))
    expect(await screen.findByRole('heading', { name: 'Đăng nhập' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/login')
  })
})
