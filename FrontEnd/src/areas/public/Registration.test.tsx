import { screen, within } from '@testing-library/react'
import { delay } from 'msw'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { setRequestTimeout } from '@/shared/api/client'
import i18n from '@/shared/i18n'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'

/** Records the JSON bodies sent to one endpoint and answers with `respond`. */
function capture(
  path: '/api/v1/auth/register' | '/api/v1/auth/resend-confirmation' | '/api/v1/auth/confirm-email',
  respond: () => Response | undefined = () => undefined,
) {
  const bodies: unknown[] = []
  server.use(
    mockApi('post', path, async ({ request }) => {
      bodies.push(await request.json())
      return respond()
    }),
  )
  return bodies
}

async function fillRegistration(user: Awaited<ReturnType<typeof renderRoutes>>['user'], password = 'Correct-horse-1') {
  await user.type(screen.getByLabelText('Email'), ' new.parent@example.test ')
  await user.type(screen.getByLabelText('Mật khẩu'), password)
  await user.type(screen.getByLabelText('Nhập lại mật khẩu'), password)
}

describe('registration', () => {
  it('registers with consent and the interface language, then asks to check the inbox', async () => {
    const bodies = capture('/api/v1/auth/register')
    const { user, router } = await renderRoutes(routes, { route: '/register' })

    await fillRegistration(user)
    await user.click(screen.getByRole('checkbox', { name: /Tôi đồng ý/ }))
    await user.click(screen.getByRole('button', { name: 'Đăng ký' }))

    expect(await screen.findByRole('heading', { name: 'Kiểm tra hộp thư của bạn' })).toBeTruthy()
    expect(router.state.location.pathname).toBe('/register/check-email')
    expect(screen.getByText(/new\.parent@example\.test/)).toBeTruthy()
    expect(bodies).toEqual([
      { email: 'new.parent@example.test', password: 'Correct-horse-1', acceptTerms: true, language: 'vi' },
    ])
  })

  it('links the consent to the terms and the privacy policy', async () => {
    await renderRoutes(routes, { route: '/register' })

    expect(screen.getByRole('link', { name: 'Điều khoản sử dụng' }).getAttribute('href')).toBe('/terms')
    expect(screen.getByRole('link', { name: 'Chính sách quyền riêng tư' }).getAttribute('href')).toBe('/privacy')
  })

  it('needs consent and a strong, repeated password before calling the API', async () => {
    const bodies = capture('/api/v1/auth/register')
    const { user } = await renderRoutes(routes, { route: '/register' })

    await user.type(screen.getByLabelText('Email'), 'parent@example.test')
    await user.type(screen.getByLabelText('Mật khẩu'), 'weak')
    await user.type(screen.getByLabelText('Nhập lại mật khẩu'), 'other')
    await user.click(screen.getByRole('button', { name: 'Đăng ký' }))

    expect(screen.getByText('Bạn cần đồng ý để tiếp tục.')).toBeTruthy()
    expect(screen.getByText('Mật khẩu chưa đủ các yêu cầu bên dưới.')).toBeTruthy()
    expect(screen.getByText('Hai mật khẩu không khớp.')).toBeTruthy()
    expect(bodies).toEqual([])
  })

  it('asks to wait after too many attempts', async () => {
    capture('/api/v1/auth/register', () => problem(429))
    const { user } = await renderRoutes(routes, { route: '/register' })

    await fillRegistration(user)
    await user.click(screen.getByRole('checkbox', { name: /Tôi đồng ý/ }))
    await user.click(screen.getByRole('button', { name: 'Đăng ký' }))

    expect((await screen.findByRole('alert')).textContent).toContain('Bạn đã thử quá nhiều lần.')
  })

  it('sends English emails when the interface is in English', async () => {
    const bodies = capture('/api/v1/auth/register')
    await i18n.changeLanguage('en')
    const { user } = await renderRoutes(routes, { route: '/register' })

    await user.type(screen.getByLabelText('Email'), 'parent@example.test')
    await user.type(screen.getByLabelText('Password'), 'Correct-horse-1')
    await user.type(screen.getByLabelText('Repeat the password'), 'Correct-horse-1')
    await user.click(screen.getByRole('checkbox', { name: /I accept/ }))
    await user.click(screen.getByRole('button', { name: 'Register' }))

    await screen.findByRole('heading', { name: 'Check your inbox' })
    expect(bodies).toMatchObject([{ language: 'en' }])
  })

  it('sends the link again from the check-email page', async () => {
    const bodies = capture('/api/v1/auth/resend-confirmation')
    const { user, router } = await renderRoutes(routes, { route: '/register' })
    capture('/api/v1/auth/register')
    await fillRegistration(user)
    await user.click(screen.getByRole('checkbox', { name: /Tôi đồng ý/ }))
    await user.click(screen.getByRole('button', { name: 'Đăng ký' }))
    await screen.findByRole('heading', { name: 'Kiểm tra hộp thư của bạn' })
    expect(router.state.location.pathname).toBe('/register/check-email')

    await user.click(screen.getByRole('button', { name: 'Gửi lại liên kết' }))

    expect((await screen.findByRole('status')).textContent).toContain('liên kết mới đã được gửi')
    expect(bodies).toEqual([{ email: 'new.parent@example.test', language: 'vi' }])
  })
})

describe('email confirmation page', () => {
  it('confirms the token once and offers to sign in', async () => {
    const bodies = capture('/api/v1/auth/confirm-email')

    await renderRoutes(routes, { route: '/confirm-email?token=abc-123' })

    expect(await screen.findByRole('heading', { name: 'Email đã được xác nhận' })).toBeTruthy()
    expect(within(screen.getByRole('main')).getByRole('link', { name: 'Đăng nhập' }).getAttribute('href')).toBe(
      '/login',
    )
    expect(bodies).toEqual([{ token: 'abc-123' }])
  })

  it('explains an invalid or expired link and lets the parent ask for a new one', async () => {
    capture('/api/v1/auth/confirm-email', () => problem(400, { errors: { Token: ['invalid'] } }))
    const resent = capture('/api/v1/auth/resend-confirmation')
    const { user } = await renderRoutes(routes, { route: '/confirm-email?token=old' })

    expect(await screen.findByRole('heading', { name: 'Liên kết không hợp lệ hoặc đã hết hạn' })).toBeTruthy()
    await user.type(screen.getByLabelText('Email'), 'parent@example.test')
    await user.click(screen.getByRole('button', { name: 'Gửi lại liên kết' }))

    await screen.findByRole('status')
    expect(resent).toEqual([{ email: 'parent@example.test', language: 'vi' }])
  })

  it('stops waiting for a server that does not answer and offers to try again', async () => {
    setRequestTimeout(50)
    let calls = 0
    server.use(
      mockApi('post', '/api/v1/auth/confirm-email', async () => {
        calls += 1
        if (calls === 1) {
          await delay('infinite')
        }
        return undefined
      }),
    )
    try {
      const { user } = await renderRoutes(routes, { route: '/confirm-email?token=abc' })

      expect((await screen.findByRole('alert')).textContent).toContain('Không xác nhận được lúc này.')
      await user.click(screen.getByRole('button', { name: 'Thử lại' }))

      expect(await screen.findByRole('heading', { name: 'Email đã được xác nhận' })).toBeTruthy()
    } finally {
      setRequestTimeout(20_000)
    }
  })

  it('treats a missing token as an invalid link without calling the API', async () => {
    const bodies = capture('/api/v1/auth/confirm-email')

    await renderRoutes(routes, { route: '/confirm-email' })

    expect(screen.getByRole('heading', { name: 'Liên kết không hợp lệ hoặc đã hết hạn' })).toBeTruthy()
    expect(bodies).toEqual([])
  })
})

describe('signing in before confirming', () => {
  it('explains the unconfirmed email and sends a new link to it', async () => {
    server.use(
      mockApi('post', '/api/v1/auth/login', () =>
        problem(403, { title: 'Access denied.', code: 'email-not-confirmed' }),
      ),
    )
    const resent = capture('/api/v1/auth/resend-confirmation')
    const { user } = await renderRoutes(routes, { route: '/login' })

    await user.type(screen.getByLabelText('Email'), 'parent@example.test')
    await user.type(screen.getByLabelText('Mật khẩu'), 'Correct-horse-1')
    await user.click(screen.getByRole('button', { name: 'Đăng nhập' }))

    expect((await screen.findByRole('alert')).textContent).toContain('Email của bạn chưa được xác nhận.')
    await user.click(screen.getByRole('button', { name: 'Gửi lại liên kết' }))
    await screen.findByRole('status')
    expect(resent).toEqual([{ email: 'parent@example.test', language: 'vi' }])
  })

  it('points to registration from the sign-in page', async () => {
    await renderRoutes(routes, { route: '/login' })

    expect(screen.getByRole('link', { name: 'Đăng ký cho phụ huynh' }).getAttribute('href')).toBe('/register')
  })
})

describe('legal pages', () => {
  it.each([
    ['/terms', 'Điều khoản sử dụng'],
    ['/privacy', 'Chính sách quyền riêng tư'],
  ])('%s shows its provisional page', async (route, title) => {
    await renderRoutes(routes, { route })

    const main = within(screen.getByRole('main'))
    expect(main.getByRole('heading', { level: 1, name: title })).toBeTruthy()
    expect(main.getByText(/đang được hoàn thiện/)).toBeTruthy()
  })
})
