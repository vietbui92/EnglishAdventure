import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type { LearnerMap, PlayLesson } from '@/areas/kids/play/api'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAsChild } from '@/shared/test/session'

const levelId = '00000000-0000-0000-0000-00000000a001'
const petsId = '00000000-0000-0000-0000-00000000b001'
const farmId = '00000000-0000-0000-0000-00000000b002'
const bossId = '00000000-0000-0000-0000-00000000b0b0'

const map: LearnerMap = {
  levels: [
    {
      id: levelId,
      title: 'Animal Island',
      description: 'Cats and dogs',
      lessons: [
        { id: petsId, title: 'Pets' },
        { id: farmId, title: 'Farm' },
      ],
      boss: { id: bossId, name: 'King Lion', imageUrl: 'https://cdn.example.test/lion.png' },
    },
  ],
}

function lesson(overrides: Partial<PlayLesson> = {}): PlayLesson {
  return {
    id: petsId,
    levelId,
    title: 'Pets',
    kind: 'Regular',
    bossImageUrl: null,
    learnCards: [
      { english: 'cat', meaning: 'con mèo', imageUrl: 'https://cdn.example.test/cat.png' },
      { english: 'dog', meaning: 'con chó', imageUrl: null },
    ],
    activities: [],
    ...overrides,
  }
}

describe('adventure map', () => {
  it('shows the levels with their lessons in order and the boss', async () => {
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    signInAsChild({ nickname: 'Bin' })
    await renderRoutes(routes, { route: '/kids' })

    expect(await screen.findByRole('heading', { name: 'Animal Island' })).toBeTruthy()
    expect(screen.getByText('Level 1')).toBeTruthy()
    const lessons = within(screen.getByRole('list', { name: 'Các bài của Animal Island' })).getAllByRole('link')
    expect(lessons.map((link) => link.textContent)).toEqual(['1Pets', '2Farm'])
    expect(lessons[0]!.getAttribute('href')).toBe(`/kids/lessons/${petsId}`)
    expect(screen.getByRole('link', { name: /King Lion/ }).getAttribute('href')).toBe(`/kids/lessons/${bossId}`)
  })

  it('says when there is nothing to play yet', async () => {
    server.use(mockApi('get', '/api/v1/play/map', () => ({ levels: [] })))
    signInAsChild()
    await renderRoutes(routes, { route: '/kids' })

    expect(await screen.findByText('Chưa có bài học nào. Bố mẹ hãy quay lại sau nhé!')).toBeTruthy()
  })

  it('offers to try again when the map does not load', async () => {
    let calls = 0
    server.use(
      mockApi('get', '/api/v1/play/map', () => {
        calls += 1
        return calls === 1 ? problem(500) : map
      }),
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: '/kids' })

    await user.click(await screen.findByRole('button', { name: 'Thử lại' }))

    expect(await screen.findByRole('heading', { name: 'Animal Island' })).toBeTruthy()
  })
})

describe('learn step', () => {
  it('shows the word cards one at a time, then leads to the games', async () => {
    server.use(mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson()))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    expect(await screen.findByRole('heading', { name: 'Pets' })).toBeTruthy()
    const first = screen.getByRole('article', { name: 'cat' })
    expect(within(first).getByRole('img', { name: 'cat' }).getAttribute('src')).toBe('https://cdn.example.test/cat.png')
    expect(within(first).getByText('con mèo')).toBeTruthy()
    expect(screen.getByText('Thẻ 1 / 2')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Trước' }).hasAttribute('disabled')).toBe(true)

    await user.click(screen.getByRole('button', { name: 'Tiếp' }))
    const second = screen.getByRole('article', { name: 'dog' })
    expect(within(second).getByRole('img', { name: 'Chưa có hình' })).toBeTruthy()
    expect(screen.getByText('Thẻ 2 / 2')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Trước' }))
    expect(screen.getByRole('article', { name: 'cat' })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Tiếp' }))
    await user.click(screen.getByRole('button', { name: 'Chơi thôi!' }))

    expect(screen.getByText('Trò chơi sắp có rồi!')).toBeTruthy()
    expect(screen.queryByRole('article')).toBeNull()
  })

  it('starts a boss with the fight, without a learn step', async () => {
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () =>
        lesson({
          id: bossId,
          title: 'King Lion',
          kind: 'Boss',
          bossImageUrl: 'https://cdn.example.test/lion.png',
          learnCards: [],
        }),
      ),
    )
    signInAsChild()
    await renderRoutes(routes, { route: `/kids/lessons/${bossId}` })

    expect(await screen.findByText('Đánh bại King Lion!')).toBeTruthy()
    expect(screen.getByRole('img', { name: 'King Lion' }).getAttribute('src')).toBe('https://cdn.example.test/lion.png')
    expect(screen.queryByText(/Thẻ 1/)).toBeNull()
  })

  it('shows not found for a lesson that is not published', async () => {
    server.use(mockApi('get', '/api/v1/play/lessons/{lessonId}', () => problem(404)))
    signInAsChild()
    const { user, router } = await renderRoutes(routes, { route: `/kids/lessons/${farmId}` })

    expect(await screen.findByText('Không tìm thấy bài học này.')).toBeTruthy()
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    await user.click(screen.getByRole('link', { name: 'Về bản đồ' }))
    expect(router.state.location.pathname).toBe('/kids')
  })
})
