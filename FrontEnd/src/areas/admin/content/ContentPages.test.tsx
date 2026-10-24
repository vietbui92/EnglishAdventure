import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import { moveInOrder, type LessonSummary, type LevelSummary } from '@/areas/admin/content/api'
import { Permissions } from '@/shared/auth/permissions'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

const levelA = '00000000-0000-0000-0000-00000000a001'
const levelB = '00000000-0000-0000-0000-00000000a002'
const levelOld = '00000000-0000-0000-0000-00000000a003'
const lessonPets = '00000000-0000-0000-0000-00000000b001'
const lessonFarm = '00000000-0000-0000-0000-00000000b002'
const bossId = '00000000-0000-0000-0000-00000000b0b0'

function level(id: string, title: string, overrides: Partial<LevelSummary> = {}): LevelSummary {
  return {
    id,
    title,
    description: null,
    sortOrder: 1,
    status: 'Draft',
    createdAtUtc: '2026-10-24T08:00:00Z',
    publishedAtUtc: null,
    archivedAtUtc: null,
    lessons: { draft: 0, published: 0, archived: 0 },
    version: `v-${id}`,
    bossStatus: null,
    ...overrides,
  }
}

function lesson(id: string, title: string, overrides: Partial<LessonSummary> = {}): LessonSummary {
  return {
    id,
    levelId: levelA,
    title,
    sortOrder: 1,
    status: 'Draft',
    createdAtUtc: '2026-10-24T08:00:00Z',
    publishedAtUtc: null,
    archivedAtUtc: null,
    version: `v-${id}`,
    kind: 'Regular',
    bossImageUrl: null,
    ...overrides,
  }
}

interface Recorded {
  method: string
  path: string
  body?: unknown
}

/** A fake content API: two levels (A with two lessons) and one archived level. Records every change. */
function serveContent() {
  const state = {
    levels: [
      level(levelA, 'Animal Island', {
        description: 'Cats and dogs',
        status: 'Published',
        lessons: { draft: 1, published: 1, archived: 0 },
      }),
      level(levelB, 'Color Garden', { sortOrder: 2 }),
      level(levelOld, 'Old Level', { status: 'Archived', archivedAtUtc: '2026-10-20T08:00:00Z' }),
    ],
    lessons: [lesson(lessonPets, 'Pets', { status: 'Published' }), lesson(lessonFarm, 'Farm', { sortOrder: 2 })],
    requests: [] as Recorded[],
    levelReads: 0,
  }
  const details = (levelId: string) => ({
    level: state.levels.find((candidate) => candidate.id === levelId)!,
    lessons: state.lessons.filter((candidate) => candidate.levelId === levelId),
  })
  const record = async (request: Request) => {
    const text = await request.text()
    state.requests.push({
      method: request.method,
      path: new URL(request.url).pathname,
      body: text ? (JSON.parse(text) as unknown) : undefined,
    })
  }

  server.use(
    mockApi('get', '/api/v1/admin/content/levels', ({ request }) => {
      const includeArchived = new URL(request.url).searchParams.get('includeArchived') === 'true'
      return state.levels.filter((candidate) => includeArchived || candidate.status !== 'Archived')
    }),
    mockApi('get', '/api/v1/admin/content/levels/{levelId}', ({ params }) => {
      state.levelReads += 1
      const levelId = String(params.levelId)
      return state.levels.some((candidate) => candidate.id === levelId)
        ? details(levelId)
        : problem(404, { title: 'Resource not found.' })
    }),
    mockApi('post', '/api/v1/admin/content/levels', async ({ request }) => {
      await record(request.clone())
      const body = (await request.json()) as { title: string; description: string }
      const created = level('00000000-0000-0000-0000-00000000a009', body.title, { sortOrder: 3 })
      state.levels.push(created)
      return { level: created, lessons: [] }
    }),
    mockApi('put', '/api/v1/admin/content/levels/order', async ({ request }) => {
      await record(request.clone())
      const { levelIds } = (await request.json()) as { levelIds: string[] }
      state.levels.sort((a, b) => levelIds.indexOf(a.id) - levelIds.indexOf(b.id))
      return state.levels.filter((candidate) => candidate.status !== 'Archived')
    }),
    mockApi('post', '/api/v1/admin/content/levels/{levelId}/lessons', async ({ request }) => {
      await record(request.clone())
      const { title } = (await request.json()) as { title: string }
      const created = lesson('00000000-0000-0000-0000-00000000b009', title, { sortOrder: 3 })
      state.lessons.push(created)
      return created
    }),
    mockApi('post', '/api/v1/admin/content/levels/{levelId}/boss', async ({ request }) => {
      await record(request.clone())
      const { title, imageUrl } = (await request.json()) as { title: string; imageUrl: string | null }
      const created = lesson(bossId, title, { kind: 'Boss', bossImageUrl: imageUrl })
      state.lessons.push(created)
      return created
    }),
    mockApi('put', '/api/v1/admin/content/levels/{levelId}/lessons/order', async ({ request, params }) => {
      await record(request)
      return details(String(params.levelId))
    }),
    mockApi('put', '/api/v1/admin/content/lessons/{lessonId}', async ({ request, params }) => {
      await record(request.clone())
      const { title } = (await request.json()) as { title: string }
      const changed = state.lessons.find((candidate) => candidate.id === params.lessonId)!
      changed.title = title
      return changed
    }),
    mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/publish', async ({ request, params }) => {
      await record(request)
      const changed = state.lessons.find((candidate) => candidate.id === params.lessonId)!
      changed.status = 'Published'
      return changed
    }),
    mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/archive', async ({ request, params }) => {
      await record(request)
      const changed = state.lessons.find((candidate) => candidate.id === params.lessonId)!
      changed.status = 'Archived'
      return changed
    }),
  )
  return state
}

const readOnlyPermissions = [Permissions.contentReadDrafts]

describe('levels list', () => {
  it('shows the levels in order with their status and lesson counts, archived ones on request', async () => {
    serveContent()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: '/admin/content' })

    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(3)
    expect(within(rows[1]!).getByRole('link', { name: 'Animal Island' })).toBeTruthy()
    expect(within(rows[1]!).getByText('Đã xuất bản')).toBeTruthy()
    expect(within(rows[1]!).getByText('1 đã xuất bản · 1 bản nháp')).toBeTruthy()
    expect(within(rows[2]!).getByText('Bản nháp')).toBeTruthy()
    expect(screen.queryByText('Old Level')).toBeNull()

    await user.click(screen.getByLabelText('Hiện cả level đã lưu trữ'))
    expect(await screen.findByText('Old Level')).toBeTruthy()
    expect(screen.getByText('Đã lưu trữ')).toBeTruthy()
  })

  it('creates a level and opens it', async () => {
    const state = serveContent()
    signInAs('ContentAdmin')
    const { user, router } = await renderRoutes(routes, { route: '/admin/content' })

    await user.click(await screen.findByRole('button', { name: 'Thêm level' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Tên'), '  Number Hill ')
    await user.type(within(dialog).getByLabelText('Mô tả'), 'One to ten')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo' }))

    await waitFor(() =>
      expect(router.state.location.pathname).toBe('/admin/content/levels/00000000-0000-0000-0000-00000000a009'),
    )
    expect(state.requests).toEqual([
      {
        method: 'POST',
        path: '/api/v1/admin/content/levels',
        body: { title: 'Number Hill', description: 'One to ten' },
      },
    ])
  })

  it('checks the title before sending anything', async () => {
    const state = serveContent()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: '/admin/content' })

    await user.click(await screen.findByRole('button', { name: 'Thêm level' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Tên'), '   ')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo' }))

    expect(await within(dialog).findByText('Hãy nhập tên (1 đến 100 ký tự).')).toBeTruthy()
    expect(state.requests).toEqual([])
  })

  it('moves a level with the full new order', async () => {
    const state = serveContent()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: '/admin/content' })

    expect((await screen.findByRole('button', { name: 'Chuyển Animal Island lên' })).hasAttribute('disabled')).toBe(
      true,
    )
    expect(screen.getByRole('button', { name: 'Chuyển Color Garden xuống' }).hasAttribute('disabled')).toBe(true)
    await user.click(screen.getByRole('button', { name: 'Chuyển Animal Island xuống' }))

    await waitFor(() =>
      expect(state.requests).toEqual([
        { method: 'PUT', path: '/api/v1/admin/content/levels/order', body: { levelIds: [levelB, levelA] } },
      ]),
    )
    await waitFor(() => expect(within(screen.getAllByRole('row')[1]!).getByText('Color Garden')).toBeTruthy())
  })

  it('shows no changes to a user who can only read drafts', async () => {
    serveContent()
    signInAs('ContentAdmin', { permissions: readOnlyPermissions })
    await renderRoutes(routes, { route: '/admin/content' })

    expect(await screen.findByText('Animal Island')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Thêm level' })).toBeNull()
    expect(screen.queryByRole('button', { name: /Chuyển/ })).toBeNull()
  })
})

describe('level detail', () => {
  it('shows the level and its lessons in order', async () => {
    serveContent()
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    expect(await screen.findByRole('heading', { name: 'Animal Island' })).toBeTruthy()
    expect(screen.getByText('Cats and dogs')).toBeTruthy()
    const rows = screen.getAllByRole('row')
    expect(within(rows[1]!).getByText('Pets')).toBeTruthy()
    expect(within(rows[2]!).getByText('Farm')).toBeTruthy()
    // The published level and its published lesson can both be unpublished.
    expect(screen.getAllByRole('button', { name: 'Gỡ xuất bản' })).toHaveLength(2)
  })

  it('adds, renames, reorders and publishes lessons with their versions', async () => {
    const state = serveContent()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    await user.click(await screen.findByRole('button', { name: 'Thêm bài học' }))
    let dialog = await screen.findByRole('dialog')
    expect(within(dialog).queryByLabelText('Mô tả')).toBeNull()
    await user.type(within(dialog).getByLabelText('Tên'), 'Zoo')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo' }))
    expect(await screen.findByText('Zoo')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Đổi tên Farm' }))
    dialog = await screen.findByRole('dialog')
    await user.clear(within(dialog).getByLabelText('Tên'))
    await user.type(within(dialog).getByLabelText('Tên'), 'Farm animals')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))
    expect(await screen.findByText('Farm animals')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Chuyển Pets xuống' }))
    const farmRow = screen.getByText('Farm animals').closest('tr')!
    await user.click(within(farmRow).getByRole('button', { name: 'Xuất bản' }))

    await waitFor(() => expect(state.requests).toHaveLength(4))
    expect(state.requests.slice(1)).toEqual([
      {
        method: 'PUT',
        path: `/api/v1/admin/content/lessons/${lessonFarm}`,
        body: { title: 'Farm animals', version: `v-${lessonFarm}` },
      },
      {
        method: 'PUT',
        path: `/api/v1/admin/content/levels/${levelA}/lessons/order`,
        body: { lessonIds: [lessonFarm, lessonPets, '00000000-0000-0000-0000-00000000b009'] },
      },
      {
        method: 'POST',
        path: `/api/v1/admin/content/lessons/${lessonFarm}/publish`,
        body: { version: `v-${lessonFarm}` },
      },
    ])
  })

  it('archives a lesson only after confirming that it is final', async () => {
    const state = serveContent()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    const farmRow = (await screen.findByText('Farm')).closest('tr')!
    await user.click(within(farmRow).getByRole('button', { name: 'Lưu trữ' }))
    const confirm = await screen.findByRole('alertdialog')
    expect(within(confirm).getByText(/không thể sửa hay khôi phục/)).toBeTruthy()

    await user.click(within(confirm).getByRole('button', { name: 'Hủy' }))
    expect(state.requests).toEqual([])

    await user.click(within(farmRow).getByRole('button', { name: 'Lưu trữ' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Lưu trữ vĩnh viễn' }))

    await waitFor(() =>
      expect(state.requests).toEqual([
        {
          method: 'POST',
          path: `/api/v1/admin/content/lessons/${lessonFarm}/archive`,
          body: { version: `v-${lessonFarm}` },
        },
      ]),
    )
    // Archived lessons have no position and no actions.
    await waitFor(() => expect(within(farmRow).getByText('Đã lưu trữ')).toBeTruthy())
    expect(within(farmRow).queryByRole('button')).toBeNull()
  })

  it('reloads and explains when someone else changed the lesson (409)', async () => {
    const state = serveContent()
    server.use(
      mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/publish', () => problem(409, { title: 'Conflict.' })),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    const farmRow = (await screen.findByText('Farm')).closest('tr')!
    const readsBefore = state.levelReads
    await user.click(within(farmRow).getByRole('button', { name: 'Xuất bản' }))

    expect(await screen.findByText(/vừa được người khác thay đổi/)).toBeTruthy()
    await waitFor(() => expect(state.levelReads).toBeGreaterThan(readsBefore))
  })

  it('explains that a level children started cannot be unpublished (422)', async () => {
    serveContent()
    server.use(
      mockApi('post', '/api/v1/admin/content/levels/{levelId}/unpublish', () =>
        problem(422, { title: 'Domain rule violation.' }),
      ),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    const header = (await screen.findByRole('heading', { name: 'Animal Island' })).parentElement!.parentElement!
    await user.click(within(header).getByRole('button', { name: 'Gỡ xuất bản' }))

    expect(await screen.findByText(/Đã có trẻ bắt đầu học/)).toBeTruthy()
  })

  it('shows an archived level as read-only', async () => {
    serveContent()
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route: `/admin/content/levels/${levelOld}` })

    expect(await screen.findByText(/Level này đã được lưu trữ/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Sửa tên và mô tả' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Thêm bài học' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Xuất bản' })).toBeNull()
  })

  it('shows not found for an unknown level', async () => {
    serveContent()
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route: '/admin/content/levels/00000000-0000-0000-0000-00000000ffff' })

    expect(await screen.findByText('Không tìm thấy level này.')).toBeTruthy()
  })

  it('hides changes from a user who can only read drafts', async () => {
    serveContent()
    signInAs('ContentAdmin', { permissions: readOnlyPermissions })
    await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    expect(await screen.findByText('Pets')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Thêm bài học' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Xuất bản' })).toBeNull()
    expect(screen.queryByRole('button', { name: /Đổi tên/ })).toBeNull()
  })
})

describe('level boss', () => {
  it('says a level needs a published boss and creates one, then opens it', async () => {
    const state = serveContent()
    // The new boss's page loads after creating it.
    server.use(
      mockApi('get', '/api/v1/admin/content/lessons/{lessonId}', () =>
        state.lessons.find((item) => item.id === bossId)!,
      ),
      mockApi('get', '/api/v1/admin/content/lessons/{lessonId}/words', () => ({
        lessonId: bossId,
        lessonStatus: 'Draft',
        lessonVersion: 'boss-v1',
        maxWords: 0,
        words: [],
      })),
      mockApi('get', '/api/v1/admin/content/lessons/{lessonId}/activities', () => ({
        lessonId: bossId,
        lessonStatus: 'Draft',
        lessonVersion: 'boss-v1',
        maxActivities: 4,
        maxQuestionsPerQuiz: 10,
        activities: [],
      })),
    )
    signInAs('ContentAdmin')
    const { user, router } = await renderRoutes(routes, { route: `/admin/content/levels/${levelB}` })

    expect(await screen.findByText('Level cần một boss đã xuất bản thì mới xuất bản được.')).toBeTruthy()
    expect(screen.getByText('Level này chưa có boss.')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Thêm boss' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Tên boss'), 'King Lion')
    await user.type(within(dialog).getByLabelText('Địa chỉ hình boss (https://)'), 'https://cdn.example.test/lion.png')
    await user.click(within(dialog).getByRole('button', { name: 'Tạo' }))

    await waitFor(() => expect(router.state.location.pathname).toBe(`/admin/content/lessons/${bossId}`))
    expect(state.requests[0]).toEqual({
      method: 'POST',
      path: `/api/v1/admin/content/levels/${levelB}/boss`,
      body: { title: 'King Lion', imageUrl: 'https://cdn.example.test/lion.png' },
    })
    expect(await screen.findByText(/Boss không có ngân hàng từ riêng/)).toBeTruthy()
  })

  it('shows the boss apart from the lessons and keeps it out of their order', async () => {
    const state = serveContent()
    state.lessons.push(
      lesson(bossId, 'King Lion', {
        kind: 'Boss',
        status: 'Published',
        bossImageUrl: 'https://cdn.example.test/lion.png',
      }),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelA}` })

    const bossSection = (await screen.findByRole('heading', { name: 'Boss cuối level' })).closest('section')!
    expect(within(bossSection).getByRole('link', { name: 'King Lion' }).getAttribute('href')).toBe(
      `/admin/content/lessons/${bossId}`,
    )
    expect(within(bossSection).queryByRole('button', { name: 'Thêm boss' })).toBeNull()
    const lessonsSection = screen.getByRole('heading', { name: 'Bài học' }).closest('section')!
    expect(within(lessonsSection).queryByText('King Lion')).toBeNull()

    await user.click(within(lessonsSection).getByRole('button', { name: 'Chuyển Pets xuống' }))
    await waitFor(() =>
      expect(state.requests).toEqual([
        {
          method: 'PUT',
          path: `/api/v1/admin/content/levels/${levelA}/lessons/order`,
          body: { lessonIds: [lessonFarm, lessonPets] },
        },
      ]),
    )
  })

  it('explains that a level is published only with its boss (422 level-needs-boss)', async () => {
    serveContent()
    server.use(
      mockApi('post', '/api/v1/admin/content/levels/{levelId}/publish', () =>
        problem(422, { title: 'Domain rule violation.', code: 'level-needs-boss' }),
      ),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route: `/admin/content/levels/${levelB}` })

    const header = (await screen.findByRole('heading', { name: 'Color Garden' })).parentElement!.parentElement!
    await user.click(within(header).getByRole('button', { name: 'Xuất bản' }))

    expect(await screen.findByText('Level chỉ xuất bản được khi boss của nó đã xuất bản.')).toBeTruthy()
  })
})

describe('content area access', () => {
  it('is closed to admins without content.read-drafts', async () => {
    serveContent()
    signInAs('SuperAdmin', { permissions: [Permissions.usersRead] })
    await renderRoutes(routes, { route: '/admin/content' })

    expect(await screen.findByRole('heading', { name: 'Không có quyền truy cập' })).toBeTruthy()
    expect(screen.queryByText('Animal Island')).toBeNull()
    expect(screen.queryByRole('link', { name: 'Nội dung' })).toBeNull()
  })
})

describe('moveInOrder', () => {
  const items = [{ id: 'a' }, { id: 'b' }, { id: 'c' }]

  it('swaps an item with its neighbour', () => {
    expect(moveInOrder(items, 0, 1)).toEqual(['b', 'a', 'c'])
    expect(moveInOrder(items, 2, -1)).toEqual(['a', 'c', 'b'])
  })

  it('refuses to move past either end', () => {
    expect(moveInOrder(items, 0, -1)).toBeNull()
    expect(moveInOrder(items, 2, 1)).toBeNull()
  })
})
