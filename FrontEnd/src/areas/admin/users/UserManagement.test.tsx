import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type { UserSummary } from '@/areas/admin/users/api'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

const targetId = '00000000-0000-0000-0000-0000000000aa'

function adminUser(overrides: Partial<UserSummary> = {}): UserSummary {
  return {
    id: targetId,
    email: 'editor@example.test',
    userType: 'Admin',
    isActive: true,
    isLockedOut: false,
    roles: ['ContentAdmin'],
    createdAtUtc: '2026-10-01T08:00:00Z',
    lastLoginAtUtc: null,
    ...overrides,
  }
}

/** A fake API for one user that applies deactivate, activate and role changes, and records the requests. */
function serveUser(initial: UserSummary) {
  const state = { user: initial, requests: [] as { method: string; path: string; body?: unknown }[] }
  server.use(
    mockApi('get', '/api/v1/admin/users/{userId}', () => state.user),
    mockApi('post', '/api/v1/admin/users/{userId}/deactivate', ({ request }) => {
      state.requests.push({ method: 'POST', path: new URL(request.url).pathname })
      state.user = { ...state.user, isActive: false }
      return undefined
    }),
    mockApi('post', '/api/v1/admin/users/{userId}/activate', ({ request }) => {
      state.requests.push({ method: 'POST', path: new URL(request.url).pathname })
      state.user = { ...state.user, isActive: true }
      return undefined
    }),
    mockApi('put', '/api/v1/admin/users/{userId}/roles', async ({ request }) => {
      const body = (await request.json()) as { roles: string[] }
      state.requests.push({ method: 'PUT', path: new URL(request.url).pathname, body })
      state.user = { ...state.user, roles: body.roles }
      return state.user
    }),
  )
  return state
}

async function openUser(id = targetId) {
  return renderRoutes(routes, { route: `/admin/users/${id}` })
}

describe('creating a user', () => {
  function serveCreate(created: (body: { email: string; userType: string }) => UserSummary) {
    const bodies: unknown[] = []
    server.use(
      mockApi('post', '/api/v1/admin/users', async ({ request }) => {
        const body = (await request.json()) as { email: string; password: string; userType: string }
        bodies.push(body)
        return created(body)
      }),
      mockApi('get', '/api/v1/admin/users/{userId}', () => created({ email: 'x', userType: 'Admin' })),
    )
    return bodies
  }

  async function fill(actor: Awaited<ReturnType<typeof openUser>>['user'], password: string, confirm = password) {
    await actor.type(screen.getByLabelText('Email'), ' new.admin@example.test ')
    await actor.type(screen.getByLabelText('Mật khẩu'), password)
    await actor.type(screen.getByLabelText('Nhập lại mật khẩu'), confirm)
  }

  it('creates an Admin user and asks for roles next', async () => {
    const bodies = serveCreate(({ email, userType }) => adminUser({ email, userType, roles: [] }))
    signInAs('SuperAdmin')
    const { user: actor, router } = await renderRoutes(routes, { route: '/admin/users/new' })

    await fill(actor, 'Correct-horse-1')
    await actor.click(screen.getByRole('button', { name: 'Tạo người dùng' }))

    expect(await screen.findByText('Đã tạo người dùng. Hãy chọn vai trò để cấp quyền.')).toBeTruthy()
    expect(router.state.location.pathname).toBe(`/admin/users/${targetId}`)
    expect(bodies).toEqual([{ email: 'new.admin@example.test', password: 'Correct-horse-1', userType: 'Admin' }])
  })

  it('creates a Parent user', async () => {
    const bodies = serveCreate(({ email, userType }) => adminUser({ email, userType, roles: ['Parent'] }))
    signInAs('SuperAdmin')
    const { user: actor } = await renderRoutes(routes, { route: '/admin/users/new' })

    await actor.click(screen.getByRole('radio', { name: 'Phụ huynh' }))
    await fill(actor, 'Correct-horse-1')
    await actor.click(screen.getByRole('button', { name: 'Tạo người dùng' }))

    expect(await screen.findByText('Đã tạo người dùng.')).toBeTruthy()
    expect(bodies).toMatchObject([{ userType: 'Parent' }])
  })

  it('shows the password rules as they are met and blocks a weak or mismatched password', async () => {
    signInAs('SuperAdmin')
    const { user: actor } = await renderRoutes(routes, { route: '/admin/users/new' })
    const rules = within(screen.getByText('Mật khẩu cần có:').parentElement!)

    await actor.type(screen.getByLabelText('Mật khẩu'), 'abc')
    expect(rules.getByText(/Một chữ thường/).textContent).toContain('đã đạt')
    expect(rules.getByText(/Một chữ in hoa/).textContent).toContain('chưa đạt')

    await actor.type(screen.getByLabelText('Nhập lại mật khẩu'), 'abd')
    await actor.click(screen.getByRole('button', { name: 'Tạo người dùng' }))

    expect(screen.getByText('Mật khẩu chưa đủ các yêu cầu bên dưới.')).toBeTruthy()
    expect(screen.getByText('Hai mật khẩu không khớp.')).toBeTruthy()
    expect(screen.getByText('Hãy nhập email.')).toBeTruthy()
  })

  it('marks an email that already has an account', async () => {
    server.use(
      mockApi('post', '/api/v1/admin/users', () => problem(409, { detail: 'A user with this email already exists.' })),
    )
    signInAs('SuperAdmin')
    const { user: actor, router } = await renderRoutes(routes, { route: '/admin/users/new' })

    await fill(actor, 'Correct-horse-1')
    await actor.click(screen.getByRole('button', { name: 'Tạo người dùng' }))

    expect(await screen.findByText('Email này đã có tài khoản.')).toBeTruthy()
    expect(screen.getByLabelText('Email').getAttribute('aria-invalid')).toBe('true')
    expect(router.state.location.pathname).toBe('/admin/users/new')
  })

  it('is offered from the list only to users who may create users', async () => {
    server.use(mockApi('get', '/api/v1/admin/users', () => ({ items: [], page: 1, pageSize: 20, totalCount: 0 })))
    signInAs('SuperAdmin', { permissions: ['users.read'] })

    const { router } = await renderRoutes(routes, { route: '/admin/users' })

    await screen.findByText('Chưa có người dùng nào.')
    expect(screen.queryByRole('link', { name: 'Thêm người dùng' })).toBeNull()
    router.navigate('/admin/users/new')
    expect(await screen.findByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
  })
})

describe('account status', () => {
  it('deactivates a user after confirmation', async () => {
    const api = serveUser(adminUser())
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('button', { name: 'Vô hiệu hóa' }))
    const dialog = await screen.findByRole('alertdialog')
    expect(within(dialog).getByText('Vô hiệu hóa editor@example.test?')).toBeTruthy()
    await actor.click(within(dialog).getByRole('button', { name: 'Vô hiệu hóa' }))

    expect(await screen.findByText('Đã vô hiệu hóa tài khoản.')).toBeTruthy()
    expect(await screen.findByRole('button', { name: 'Kích hoạt lại' })).toBeTruthy()
    expect(api.requests).toEqual([{ method: 'POST', path: `/api/v1/admin/users/${targetId}/deactivate` }])
  })

  it('does nothing when the confirmation is cancelled', async () => {
    const api = serveUser(adminUser())
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('button', { name: 'Vô hiệu hóa' }))
    await actor.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Hủy' }))

    expect(screen.queryByRole('alertdialog')).toBeNull()
    expect(api.requests).toEqual([])
  })

  it('explains that the last SuperAdmin cannot be deactivated', async () => {
    serveUser(adminUser({ roles: ['SuperAdmin'] }))
    server.use(mockApi('post', '/api/v1/admin/users/{userId}/deactivate', () => problem(422)))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('button', { name: 'Vô hiệu hóa' }))
    await actor.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Vô hiệu hóa' }))

    expect(
      await screen.findByText('Không thể vô hiệu hóa: đây là Quản trị cấp cao cuối cùng còn hoạt động.'),
    ).toBeTruthy()
  })

  it('reactivates a deactivated user', async () => {
    const api = serveUser(adminUser({ isActive: false }))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('button', { name: 'Kích hoạt lại' }))

    expect(await screen.findByText('Đã kích hoạt lại tài khoản.')).toBeTruthy()
    expect(api.requests).toEqual([{ method: 'POST', path: `/api/v1/admin/users/${targetId}/activate` }])
  })

  it('offers no changes to your own account', async () => {
    const me = signInAs('SuperAdmin')
    serveUser(adminUser({ id: me.user.userId, roles: ['SuperAdmin'] }))

    await openUser(me.user.userId)

    expect((await screen.findAllByText('Bạn không thể tự vô hiệu hóa hoặc đổi vai trò của chính mình.')).length).toBe(2)
    expect(screen.queryByRole('button', { name: 'Vô hiệu hóa' })).toBeNull()
    expect(screen.queryByRole('checkbox')).toBeNull()
  })

  it('is hidden from users without users.manage', async () => {
    serveUser(adminUser())
    signInAs('SuperAdmin', { permissions: ['users.read'] })

    await openUser()

    expect(await screen.findByRole('heading', { name: 'editor@example.test' })).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'Trạng thái tài khoản' })).toBeNull()
    expect(screen.queryByRole('checkbox')).toBeNull()
  })
})

describe('roles', () => {
  it('saves the chosen roles of an Admin user', async () => {
    const api = serveUser(adminUser({ roles: ['ContentAdmin'] }))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    const save = await screen.findByRole('button', { name: 'Lưu vai trò' })
    expect((save as HTMLButtonElement).disabled).toBe(true)
    await actor.click(screen.getByRole('checkbox', { name: 'Quản trị cấp cao' }))
    await actor.click(save)

    expect(await screen.findByText('Đã lưu vai trò.')).toBeTruthy()
    expect(api.requests).toEqual([
      { method: 'PUT', path: `/api/v1/admin/users/${targetId}/roles`, body: { roles: ['SuperAdmin', 'ContentAdmin'] } },
    ])
  })

  it('warns before saving no roles at all', async () => {
    serveUser(adminUser({ roles: ['ContentAdmin'] }))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('checkbox', { name: 'Quản trị nội dung' }))

    expect(screen.getByText('Không có vai trò nào thì người dùng không dùng được chức năng quản trị nào.')).toBeTruthy()
  })

  it('keeps a role it does not know about', async () => {
    const api = serveUser(adminUser({ roles: ['Reviewers'] }))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('checkbox', { name: 'Quản trị nội dung' }))
    await actor.click(screen.getByRole('button', { name: 'Lưu vai trò' }))

    await screen.findByText('Đã lưu vai trò.')
    expect(api.requests[0]?.body).toEqual({ roles: ['ContentAdmin', 'Reviewers'] })
  })

  it('explains that the last SuperAdmin keeps the role', async () => {
    serveUser(adminUser({ roles: ['SuperAdmin'] }))
    server.use(mockApi('put', '/api/v1/admin/users/{userId}/roles', () => problem(422)))
    signInAs('SuperAdmin')
    const { user: actor } = await openUser()

    await actor.click(await screen.findByRole('checkbox', { name: 'Quản trị cấp cao' }))
    await actor.click(screen.getByRole('button', { name: 'Lưu vai trò' }))

    expect(
      await screen.findByText('Không thể bỏ vai trò Quản trị cấp cao của người cuối cùng còn hoạt động.'),
    ).toBeTruthy()
  })

  it('cannot be edited for a Parent user', async () => {
    serveUser(adminUser({ userType: 'Parent', roles: ['Parent'] }))
    signInAs('SuperAdmin')

    await openUser()

    expect(await screen.findByText('Tài khoản phụ huynh luôn có vai trò Phụ huynh.')).toBeTruthy()
    expect(screen.queryByRole('checkbox')).toBeNull()
  })
})
