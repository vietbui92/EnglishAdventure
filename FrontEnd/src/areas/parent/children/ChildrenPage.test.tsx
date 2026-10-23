import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type { ChildProfile } from '@/areas/parent/children/api'
import { mockApi, mockFamilyWithoutChildren, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

const maxActive = 3

/**
 * A fake family API with the backend's rules: at most three active profiles (422), nicknames unique among active
 * profiles regardless of letter case (409). Records the bodies it receives.
 */
function serveFamily(initial: Partial<ChildProfile>[] = []) {
  let nextId = 1
  const profile = (values: Partial<ChildProfile>): ChildProfile => ({
    id: `00000000-0000-0000-0000-${String(nextId++).padStart(12, '0')}`,
    nickname: 'Bin',
    avatarKey: 'fox',
    status: 'Active',
    createdAtUtc: '2026-10-24T08:00:00Z',
    archivedAtUtc: null,
    ...values,
  })
  const state = { profiles: initial.map(profile), bodies: [] as unknown[] }
  const active = () => state.profiles.filter((candidate) => candidate.status === 'Active')
  const taken = (nickname: string, exceptId?: string) =>
    active().some(
      (candidate) => candidate.id !== exceptId && candidate.nickname.toLowerCase() === nickname.toLowerCase(),
    )
  const find = (id: unknown) => state.profiles.find((candidate) => candidate.id === id)

  server.use(
    mockFamilyWithoutChildren()[1]!,
    mockApi('get', '/api/v1/family/children', () => [
      ...active(),
      ...state.profiles.filter((p) => p.status === 'Archived'),
    ]),
    mockApi('post', '/api/v1/family/children', async ({ request }) => {
      const body = (await request.json()) as { nickname: string; avatarKey: string }
      state.bodies.push(body)
      if (active().length >= maxActive) return problem(422)
      if (taken(body.nickname)) return problem(409)
      const created = profile({ nickname: body.nickname.trim(), avatarKey: body.avatarKey })
      state.profiles.push(created)
      return created
    }),
    mockApi('put', '/api/v1/family/children/{profileId}', async ({ request, params }) => {
      const body = (await request.json()) as { nickname: string; avatarKey: string }
      state.bodies.push(body)
      const target = find(params.profileId)!
      if (taken(body.nickname, target.id)) return problem(409)
      Object.assign(target, { nickname: body.nickname.trim(), avatarKey: body.avatarKey })
      return target
    }),
    mockApi('post', '/api/v1/family/children/{profileId}/archive', ({ params }) => {
      const target = find(params.profileId)!
      Object.assign(target, { status: 'Archived', archivedAtUtc: '2026-10-24T09:00:00Z' })
      return target
    }),
    mockApi('post', '/api/v1/family/children/{profileId}/restore', ({ params }) => {
      const target = find(params.profileId)!
      if (active().length >= maxActive) return problem(422)
      if (taken(target.nickname, target.id)) return problem(409)
      Object.assign(target, { status: 'Active', archivedAtUtc: null })
      return target
    }),
  )
  return state
}

async function openChildren() {
  signInAs('Parent')
  const rendered = await renderRoutes(routes, { route: '/parent/children' })
  await screen.findByRole('heading', { level: 1, name: 'Con của tôi' })
  return rendered
}

function cardOf(nickname: string) {
  return screen.getByRole('heading', { level: 2, name: nickname }).closest('li')!
}

describe('ChildrenPage', () => {
  it('invites the parent to create the first profile', async () => {
    const api = serveFamily()
    const { user } = await openChildren()

    expect(screen.getByText('Chưa có hồ sơ nào')).toBeTruthy()
    expect(screen.getByText('0/3 hồ sơ đang dùng')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Thêm hồ sơ' }))
    const dialog = within(await screen.findByRole('dialog'))
    await user.type(dialog.getByLabelText('Biệt danh'), ' Bông ')
    await user.click(dialog.getByRole('radio', { name: 'Cú' }))
    await user.click(dialog.getByRole('button', { name: 'Tạo hồ sơ' }))

    expect(await screen.findByRole('heading', { level: 2, name: 'Bông' })).toBeTruthy()
    expect(screen.queryByRole('dialog')).toBeNull()
    expect(screen.getByText('1/3 hồ sơ đang dùng')).toBeTruthy()
    expect(api.bodies).toEqual([{ nickname: 'Bông', avatarKey: 'owl' }])
  })

  it('lets a family have several children and stops at three', async () => {
    serveFamily([{ nickname: 'An' }, { nickname: 'Bình' }, { nickname: 'Châu' }])
    await openChildren()

    expect(screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)).toEqual([
      'An',
      'Bình',
      'Châu',
    ])
    expect((screen.getByRole('button', { name: 'Thêm hồ sơ' }) as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByText('Gia đình đã có đủ 3 hồ sơ. Lưu trữ một hồ sơ để thêm hồ sơ mới.')).toBeTruthy()
  })

  it('needs a nickname and explains one that is already used in the family', async () => {
    const api = serveFamily([{ nickname: 'Bin' }])
    const { user } = await openChildren()
    await user.click(screen.getByRole('button', { name: 'Thêm hồ sơ' }))
    const dialog = within(await screen.findByRole('dialog'))

    await user.click(dialog.getByRole('button', { name: 'Tạo hồ sơ' }))
    expect(dialog.getByText('Hãy nhập biệt danh.')).toBeTruthy()
    expect(api.bodies).toEqual([])

    await user.type(dialog.getByLabelText('Biệt danh'), 'BIN')
    await user.click(dialog.getByRole('button', { name: 'Tạo hồ sơ' }))
    expect(await dialog.findByText('Gia đình đã có một bé dùng biệt danh này.')).toBeTruthy()
    expect(dialog.getByLabelText('Biệt danh').getAttribute('aria-invalid')).toBe('true')
  })

  it('edits a profile with its current values filled in', async () => {
    const api = serveFamily([{ nickname: 'Bin', avatarKey: 'fox' }])
    const { user } = await openChildren()

    await user.click(within(cardOf('Bin')).getByRole('button', { name: 'Sửa' }))
    const dialog = within(await screen.findByRole('dialog'))
    expect((dialog.getByLabelText('Biệt danh') as HTMLInputElement).value).toBe('Bin')
    expect(dialog.getByRole('radio', { name: 'Cáo' }).getAttribute('aria-checked')).toBe('true')

    await user.clear(dialog.getByLabelText('Biệt danh'))
    await user.type(dialog.getByLabelText('Biệt danh'), 'Bin Bin')
    await user.click(dialog.getByRole('radio', { name: 'Gấu trúc' }))
    await user.click(dialog.getByRole('button', { name: 'Lưu' }))

    expect(await screen.findByRole('heading', { level: 2, name: 'Bin Bin' })).toBeTruthy()
    expect(api.bodies).toEqual([{ nickname: 'Bin Bin', avatarKey: 'panda' }])
  })

  it('archives a profile after confirmation and restores it', async () => {
    serveFamily([{ nickname: 'Bin' }])
    const { user } = await openChildren()

    await user.click(within(cardOf('Bin')).getByRole('button', { name: 'Lưu trữ' }))
    const confirm = within(await screen.findByRole('alertdialog'))
    expect(confirm.getByText('Lưu trữ hồ sơ của Bin?')).toBeTruthy()
    await user.click(confirm.getByRole('button', { name: 'Lưu trữ' }))

    const archived = within(await screen.findByRole('region', { name: 'Hồ sơ đã lưu trữ' }))
    expect(archived.getByText('Bin')).toBeTruthy()
    expect(screen.getByText('Chưa có hồ sơ nào')).toBeTruthy()

    await user.click(archived.getByRole('button', { name: 'Khôi phục' }))
    expect(await screen.findByRole('heading', { level: 2, name: 'Bin' })).toBeTruthy()
    expect(screen.queryByRole('region', { name: 'Hồ sơ đã lưu trữ' })).toBeNull()
  })

  it('explains why an archived profile cannot be restored', async () => {
    serveFamily([
      { nickname: 'An' },
      { nickname: 'Bình' },
      { nickname: 'Châu' },
      { nickname: 'Dung', status: 'Archived' },
    ])
    const { user } = await openChildren()

    const archived = within(screen.getByRole('region', { name: 'Hồ sơ đã lưu trữ' }))
    await user.click(archived.getByRole('button', { name: 'Khôi phục' }))

    expect((await archived.findByRole('alert')).textContent).toBe(
      'Không khôi phục được: gia đình đã có đủ 3 hồ sơ đang dùng.',
    )
  })
})
