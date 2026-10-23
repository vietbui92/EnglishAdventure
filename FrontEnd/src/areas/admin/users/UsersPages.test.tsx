import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type { UserSummary } from '@/areas/admin/users/api'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

function user(index: number, overrides: Partial<UserSummary> = {}): UserSummary {
  return {
    id: `00000000-0000-0000-0000-${String(index).padStart(12, '0')}`,
    email: `user${index}@example.test`,
    userType: 'Admin',
    isActive: true,
    isLockedOut: false,
    roles: ['ContentAdmin'],
    createdAtUtc: '2026-10-01T08:00:00Z',
    lastLoginAtUtc: null,
    ...overrides,
  }
}

/** Serves `all` in pages, filtered by email like the API, and records each query string. */
function serveUsers(all: UserSummary[], seen: URLSearchParams[] = []) {
  server.use(
    mockApi('get', '/api/v1/admin/users', ({ request }) => {
      const query = new URL(request.url).searchParams
      seen.push(query)
      const page = Number(query.get('page') ?? 1)
      const pageSize = Number(query.get('pageSize') ?? 20)
      const search = query.get('search') ?? ''
      const matching = all.filter((candidate) => candidate.email.includes(search))
      return {
        items: matching.slice((page - 1) * pageSize, page * pageSize),
        page,
        pageSize,
        totalCount: matching.length,
      }
    }),
  )
  return seen
}

function rows() {
  return screen.getAllByRole('row').slice(1)
}

describe('UsersListPage', () => {
  it('lists users with type, roles and status', async () => {
    serveUsers([
      user(1, { email: 'admin@example.test', roles: ['SuperAdmin'], lastLoginAtUtc: '2026-10-20T09:30:00Z' }),
      user(2, { email: 'parent@example.test', userType: 'Parent', roles: ['Parent'], isActive: false }),
      user(3, { email: 'locked@example.test', roles: [], isLockedOut: true }),
    ])
    signInAs('SuperAdmin')

    await renderRoutes(routes, { route: '/admin/users' })

    expect(await screen.findByRole('link', { name: 'admin@example.test' })).toBeTruthy()
    const [admin, parent, locked] = rows().map((row) => within(row))
    expect(admin?.getByText('Quản trị cấp cao')).toBeTruthy()
    expect(admin?.getByText('Đang hoạt động')).toBeTruthy()
    expect(parent?.getByText('Phụ huynh', { selector: 'td' })).toBeTruthy()
    expect(parent?.getByText('Đã vô hiệu hóa')).toBeTruthy()
    expect(parent?.getByText('Chưa đăng nhập')).toBeTruthy()
    expect(locked?.getByText('Đang tạm khóa')).toBeTruthy()
    expect(locked?.getByText('Không có vai trò')).toBeTruthy()
    expect(screen.getByText('Tổng: 3')).toBeTruthy()
  })

  it('searches by email and keeps the search in the address', async () => {
    const seen = serveUsers([user(1, { email: 'an@example.test' }), user(2, { email: 'binh@example.test' })])
    signInAs('SuperAdmin')
    const { user: actor, router } = await renderRoutes(routes, { route: '/admin/users' })
    await screen.findByRole('link', { name: 'an@example.test' })

    await actor.type(screen.getByRole('searchbox', { name: 'Tìm theo email' }), '  binh ')
    await actor.click(screen.getByRole('button', { name: 'Tìm' }))

    expect(await screen.findByText('Tổng: 1')).toBeTruthy()
    expect(rows()).toHaveLength(1)
    expect(router.state.location.search).toBe('?q=binh')
    expect(seen.at(-1)?.get('search')).toBe('binh')

    await actor.click(screen.getByRole('button', { name: 'Xóa tìm kiếm' }))
    expect(await screen.findByText('Tổng: 2')).toBeTruthy()
    expect(router.state.location.search).toBe('')
  })

  it('says when no user matches the search', async () => {
    serveUsers([user(1)])
    signInAs('SuperAdmin')

    await renderRoutes(routes, { route: '/admin/users?q=nobody' })

    expect(await screen.findByText('Không có người dùng nào khớp với “nobody”.')).toBeTruthy()
    expect((screen.getByRole('searchbox') as HTMLInputElement).value).toBe('nobody')
  })

  it('pages through the users, 20 at a time', async () => {
    const seen = serveUsers(Array.from({ length: 45 }, (_, index) => user(index + 1)))
    signInAs('SuperAdmin')
    const { user: actor, router } = await renderRoutes(routes, { route: '/admin/users' })

    expect(await screen.findByText('Trang 1 / 3')).toBeTruthy()
    expect(rows()).toHaveLength(20)
    expect(seen[0]?.get('pageSize')).toBe('20')
    expect((screen.getByRole('button', { name: 'Trang trước' }) as HTMLButtonElement).disabled).toBe(true)

    await actor.click(screen.getByRole('button', { name: 'Trang sau' }))
    expect(await screen.findByText('Trang 2 / 3')).toBeTruthy()
    expect(router.state.location.search).toBe('?page=2')

    await actor.click(screen.getByRole('button', { name: 'Trang sau' }))
    expect(await screen.findByText('Trang 3 / 3')).toBeTruthy()
    expect(rows()).toHaveLength(5)
    expect((screen.getByRole('button', { name: 'Trang sau' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('offers a retry when the list cannot be loaded', async () => {
    let calls = 0
    server.use(
      mockApi('get', '/api/v1/admin/users', () => {
        calls += 1
        return calls === 1 ? problem(500) : { items: [user(1)], page: 1, pageSize: 20, totalCount: 1 }
      }),
    )
    signInAs('SuperAdmin')
    const { user: actor } = await renderRoutes(routes, { route: '/admin/users' })

    expect((await screen.findByRole('alert')).textContent).toContain('Không tải được danh sách người dùng.')
    await actor.click(screen.getByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByRole('link', { name: 'user1@example.test' })).toBeTruthy()
  })

  it('opens a user from the list', async () => {
    const target = user(7, { email: 'target@example.test' })
    serveUsers([target])
    server.use(mockApi('get', '/api/v1/admin/users/{userId}', () => target))
    signInAs('SuperAdmin')
    const { user: actor, router } = await renderRoutes(routes, { route: '/admin/users' })

    await actor.click(await screen.findByRole('link', { name: 'target@example.test' }))

    expect(await screen.findByRole('heading', { name: 'target@example.test' })).toBeTruthy()
    expect(router.state.location.pathname).toBe(`/admin/users/${target.id}`)
  })
})

describe('UserDetailPage', () => {
  it('shows the user to a reader', async () => {
    const shown = user(9, { email: 'detail@example.test', roles: ['SuperAdmin', 'Reviewers'] })
    const requested: string[] = []
    server.use(
      mockApi('get', '/api/v1/admin/users/{userId}', ({ params }) => {
        requested.push(String(params.userId))
        return shown
      }),
    )
    signInAs('SuperAdmin', { permissions: ['users.read'] })

    await renderRoutes(routes, { route: `/admin/users/${shown.id}` })

    expect(await screen.findByRole('heading', { name: 'detail@example.test' })).toBeTruthy()
    expect(requested).toEqual([shown.id])
    expect(screen.getByText('Quản trị cấp cao')).toBeTruthy()
    // A role created at runtime keeps its own name.
    expect(screen.getByText('Reviewers')).toBeTruthy()
    expect(screen.getByText(shown.id)).toBeTruthy()
  })

  it('says when the user does not exist', async () => {
    server.use(mockApi('get', '/api/v1/admin/users/{userId}', () => problem(404)))
    signInAs('SuperAdmin')

    await renderRoutes(routes, { route: '/admin/users/00000000-0000-0000-0000-000000000000' })

    expect((await screen.findByRole('alert')).textContent).toContain('Không tìm thấy người dùng này.')
    expect(screen.queryByRole('button', { name: 'Thử lại' })).toBeNull()
  })

  it('is not open to a ContentAdmin', async () => {
    signInAs('ContentAdmin')

    await renderRoutes(routes, { route: '/admin/users/00000000-0000-0000-0000-000000000009' })

    expect(screen.getByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
  })
})
