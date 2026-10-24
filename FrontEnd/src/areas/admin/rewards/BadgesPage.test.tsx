import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type { BadgeDefinition } from '@/areas/admin/rewards/api'
import { mockApi } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

const badges: BadgeDefinition[] = [
  {
    kind: 'FirstLesson',
    perLevel: false,
    name: 'Bài học đầu tiên',
    description: 'Hoàn thành bài học đầu tiên.',
    imageUrl: null,
  },
  { kind: 'Perfect', perLevel: false, name: 'Hoàn hảo', description: 'Đạt 3 sao ở một bài học.', imageUrl: null },
  {
    kind: 'LevelConqueror',
    perLevel: true,
    name: 'Chinh phục {level}',
    description: 'Đánh bại boss của {level}.',
    imageUrl: null,
  },
  {
    kind: 'LevelSuperstar',
    perLevel: true,
    name: 'Siêu sao {level}',
    description: 'Đạt 3 sao ở mọi bài và boss của {level}.',
    imageUrl: null,
  },
]

describe('badges page', () => {
  it('lists the four badges and edits one', async () => {
    let saved: { kind: string; body: unknown } | null = null
    server.use(
      mockApi('get', '/api/v1/admin/rewards/badges', () => badges),
      mockApi('put', '/api/v1/admin/rewards/badges/{kind}', async ({ request, params }) => {
        const body = (await request.json()) as { name: string; description: string; imageUrl: string | null }
        saved = { kind: String(params.kind), body }
        return { ...badges[2]!, ...body }
      }),
    )
    signInAs('SuperAdmin')
    const { user } = await renderRoutes(routes, { route: '/admin/rewards' })

    expect(await screen.findByRole('heading', { name: 'Huy hiệu', level: 1 })).toBeTruthy()
    expect(screen.getAllByRole('listitem')).toHaveLength(4)
    expect(screen.getByText('Chinh phục level · mỗi level một huy hiệu')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Sửa huy hiệu Chinh phục {level}' }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText(/Viết \{level\} ở chỗ cần tên đảo/)).toBeTruthy()
    const name = within(dialog).getByLabelText('Tên huy hiệu')
    await user.clear(name)
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))
    expect(await within(dialog).findByText('Hãy nhập tên (1 đến 60 ký tự).')).toBeTruthy()

    await user.type(name, 'Vua {level}')
    await user.type(
      within(dialog).getByLabelText('Địa chỉ hình (https://, không bắt buộc)'),
      'https://cdn.example.test/crown.png',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(saved).toEqual({
      kind: 'LevelConqueror',
      body: {
        name: 'Vua {level}',
        description: 'Đánh bại boss của {level}.',
        imageUrl: 'https://cdn.example.test/crown.png',
      },
    })
  })

  it('is only for those who manage rewards', async () => {
    signInAs('ContentAdmin')
    server.use(mockApi('get', '/api/v1/admin/content/levels', () => []))
    await renderRoutes(routes, { route: '/admin' })

    expect(screen.queryByRole('link', { name: 'Huy hiệu' })).toBeNull()
  })
})
