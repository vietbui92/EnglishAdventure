import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/app/routes'
import type { AttemptView, LearnerMap, PlayLesson } from '@/areas/kids/play/api'
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
      {
        english: 'cat',
        meaning: 'con mèo',
        imageUrl: 'https://cdn.example.test/cat.png',
        movement: 'Walk',
        soundUrl: null,
      },
      { english: 'dog', meaning: 'con chó', imageUrl: null, movement: 'Still', soundUrl: null },
    ],
    activities: [],
    ...overrides,
  }
}

// Where the map remembers the default test child (signInAsChild) is standing.
const placeKey = 'kids.adventure.00000000-0000-0000-0000-0000000000c1'

describe('adventure map', () => {
  it('starts on the mainland and sails to an island with its lessons and boss', async () => {
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    signInAsChild({ nickname: 'Bin' })
    await renderRoutes(routes, { route: '/kids' })

    expect(await screen.findByRole('heading', { name: 'Xin chào, Bin!' })).toBeTruthy()
    expect(screen.getByText('Chạm vào một hòn đảo để chèo thuyền tới đó nhé!')).toBeTruthy()
    const islands = within(screen.getByRole('list', { name: 'Bản đồ phiêu lưu' })).getAllByRole('button')
    expect(islands.map((island) => island.getAttribute('aria-label'))).toEqual(['Đảo 1: Animal Island'])
    // The sign: the level's title, and the island's name from the level's description.
    expect(within(islands[0]!).getByText('Level 1 · Animal Island')).toBeTruthy()
    expect(within(islands[0]!).getByText('Cats and dogs')).toBeTruthy()
  })

  it('does not repeat "Level n" when that is already the title of the level', async () => {
    server.use(
      mockApi('get', '/api/v1/play/map', () => ({
        levels: [{ ...map.levels[0]!, title: 'Level 1', description: 'Đảo Thú Cưng' }],
      })),
    )
    signInAsChild()
    await renderRoutes(routes, { route: '/kids' })

    const island = await screen.findByRole('button', { name: 'Đảo 1: Level 1' })
    expect(within(island).getByText('Level 1')).toBeTruthy()
    expect(within(island).queryByText(/Level 1 · /)).toBeNull()
    expect(within(island).getByText('Đảo Thú Cưng')).toBeTruthy()
  })

  it('walks from the island to a lesson and opens it', async () => {
    server.use(
      mockApi('get', '/api/v1/play/map', () => map),
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson()),
    )
    signInAsChild()
    const { user, router } = await renderRoutes(routes, { route: '/kids' })

    await user.click(await screen.findByRole('button', { name: 'Đảo 1: Animal Island' }))

    expect(await screen.findByRole('heading', { name: /Animal Island/ })).toBeTruthy()
    const lessons = within(screen.getByRole('list', { name: 'Các bài của Animal Island' })).getAllByRole('link')
    expect(lessons.map((link) => link.getAttribute('aria-label'))).toEqual(['Bài 1: Pets', 'Bài 2: Farm'])
    expect(lessons[0]!.getAttribute('href')).toBe(`/kids/lessons/${petsId}`)
    expect(screen.getByRole('link', { name: 'Boss: King Lion' }).getAttribute('href')).toBe(`/kids/lessons/${bossId}`)

    await user.click(lessons[0]!)

    expect(await screen.findByRole('heading', { name: 'Pets' })).toBeTruthy()
    expect(router.state.location.pathname).toBe(`/kids/lessons/${petsId}`)
    expect(JSON.parse(localStorage.getItem(placeKey)!)).toEqual({ levelId, lessonId: petsId })
  })

  it('opens on the island where the child left, and sails back out to sea', async () => {
    localStorage.setItem(placeKey, JSON.stringify({ levelId, lessonId: farmId }))
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: '/kids' })

    expect(await screen.findByRole('heading', { name: /Animal Island/ })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Lên thuyền ra biển' }))

    expect(await screen.findByRole('button', { name: 'Đảo 1: Animal Island' })).toBeTruthy()
    expect(JSON.parse(localStorage.getItem(placeKey)!)).toEqual({ levelId, lessonId: null })
  })

  it('starts on the mainland when the remembered island is no longer published', async () => {
    localStorage.setItem(placeKey, JSON.stringify({ levelId: farmId, lessonId: null }))
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    signInAsChild()
    await renderRoutes(routes, { route: '/kids' })

    expect(await screen.findByRole('button', { name: 'Đảo 1: Animal Island' })).toBeTruthy()
  })

  it('turns the sound off and on, remembered on the device', async () => {
    server.use(mockApi('get', '/api/v1/play/map', () => map))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: '/kids' })

    const sound = await screen.findByRole('button', { name: 'Âm thanh' })
    expect(sound.getAttribute('aria-pressed')).toBe('true')
    await user.click(sound)
    expect(sound.getAttribute('aria-pressed')).toBe('false')
    expect(localStorage.getItem('kids.sound')).toBe('off')
    await user.click(sound)
    expect(sound.getAttribute('aria-pressed')).toBe('true')
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

    expect(await screen.findByRole('button', { name: 'Đảo 1: Animal Island' })).toBeTruthy()
  })
})

/** A browser that hears the child say the next answer in `heard` (its guesses, best first). */
class FakeRecognition {
  static heard: string[] = []
  lang = ''
  interimResults = true
  continuous = true
  maxAlternatives = 1
  onresult: ((event: { results: { transcript: string }[][] }) => void) | null = null
  onerror: ((event: { error: string }) => void) | null = null
  onend: (() => void) | null = null
  start() {
    queueMicrotask(() => {
      this.onresult?.({ results: [FakeRecognition.heard.map((transcript) => ({ transcript }))] })
      this.onend?.()
    })
  }
  abort() {}
}

afterEach(() => vi.unstubAllGlobals())

describe('discovery walk', () => {
  it('meets each word moving, with its sound and name, then goes to the review cards', async () => {
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () =>
        lesson({
          learnCards: [
            {
              english: 'cat',
              meaning: 'con mèo',
              imageUrl: 'https://cdn.example.test/cat.png',
              movement: 'Walk',
              soundUrl: 'https://cdn.example.test/meow.mp3',
            },
            { english: 'dog', meaning: 'con chó', imageUrl: null, movement: 'Still', soundUrl: null },
          ],
        }),
      ),
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    expect(await screen.findByText('Khu rừng bí ẩn có những người bạn đang trốn. Cùng đi tìm nhé!')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Vào rừng thôi!' }))

    expect(await screen.findByText('Bạn thứ 1 / 2')).toBeTruthy()
    const cat = screen.getByRole('article', { name: 'cat' })
    expect(within(cat).getByText('con mèo')).toBeTruthy()
    expect(within(cat).getByRole('button', { name: 'Nghe' })).toBeTruthy()
    expect(within(cat).getByRole('button', { name: 'Tiếng kêu' })).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Đi tiếp' }))
    expect(await screen.findByText('Bạn thứ 2 / 2')).toBeTruthy()
    const dog = screen.getByRole('article', { name: 'dog' })
    expect(within(dog).queryByRole('button', { name: 'Tiếng kêu' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Xem thẻ luôn' })).toBeNull()

    await user.click(screen.getByRole('button', { name: 'Ôn lại thẻ' }))
    expect(screen.getByText('Thẻ 1 / 2')).toBeTruthy()
  })

  it('can go straight to the cards', async () => {
    server.use(mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson()))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    await user.click(await screen.findByRole('button', { name: 'Xem thẻ luôn' }))

    expect(screen.getByText('Thẻ 1 / 2')).toBeTruthy()
  })
})

describe('review cards', () => {
  it('practises saying each word where the browser can hear the child', async () => {
    vi.stubGlobal('webkitSpeechRecognition', FakeRecognition)
    server.use(mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson()))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })
    await user.click(await screen.findByRole('button', { name: 'Xem thẻ luôn' }))

    expect(screen.getByText('Nghe rồi bấm micro và nói to từ này nhé!')).toBeTruthy()
    FakeRecognition.heard = ['a cat', 'hat']
    await user.click(screen.getByRole('button', { name: 'Bé nói thử' }))
    expect(await screen.findByText('Giỏi quá! Bé nói đúng rồi! 🎉')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Tiếp' }))
    FakeRecognition.heard = ['duck', 'dug']
    await user.click(screen.getByRole('button', { name: 'Bé nói thử' }))
    expect(await screen.findByText('Máy nghe thành “duck”. Nghe lại rồi nói lại nhé!')).toBeTruthy()
  })

  it('shows the word cards one at a time, then leads to the games', async () => {
    server.use(mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson()))
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    expect(await screen.findByRole('heading', { name: 'Pets' })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Xem thẻ luôn' }))
    const first = screen.getByRole('article', { name: 'cat' })
    // Without speech recognition there is only "Listen".
    expect(screen.queryByRole('button', { name: 'Bé nói thử' })).toBeNull()
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
    expect(screen.queryByRole('button', { name: 'Vào chơi luôn' })).toBeNull()
    await user.click(screen.getByRole('button', { name: 'Chơi thôi!' }))

    expect(screen.getByRole('button', { name: 'Bắt đầu chơi' })).toBeTruthy()
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
    expect(screen.getByRole('button', { name: 'Đánh boss!' })).toBeTruthy()
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

const quizId = '00000000-0000-0000-0000-00000000c001'
const matchingId = '00000000-0000-0000-0000-00000000c002'
const option = (id: string, text: string | null, imageUrl: string | null = null) => ({ id, text, imageUrl })

/** Two quiz questions, then a matching of two words with their meanings. */
const games: PlayLesson['activities'] = [
  {
    id: quizId,
    type: 'Quiz',
    matching: null,
    questions: [
      {
        id: 'q1',
        form: 'PictureToWord',
        promptText: null,
        promptImageUrl: 'https://cdn.example.test/cat.png',
        options: [option('q1-dog', 'dog'), option('q1-cat', 'cat')],
      },
      {
        id: 'q2',
        form: 'FillInTheBlank',
        promptText: 'The ___ barks.',
        promptImageUrl: null,
        options: [option('q2-dog', 'dog'), option('q2-cat', 'cat')],
      },
    ],
  },
  {
    id: matchingId,
    type: 'Matching',
    questions: [],
    matching: {
      mode: 'WordToMeaning',
      left: [option('l-cat', 'cat'), option('l-dog', 'dog')],
      right: [option('r-dog', 'con chó'), option('r-cat', 'con mèo')],
    },
  },
]

/** The answers the fake server grades with: question → right option, left word → its partner. */
const answers: Record<string, string> = { q1: 'q1-cat', q2: 'q2-dog', 'l-cat': 'r-cat', 'l-dog': 'r-dog' }

/**
 * A fake of the attempts API (S4.1) for one lesson: the first answer to a question counts, a match counts if right on
 * its first try, and the stars follow 60/80/100 (PD-011). Starting resumes the unfinished attempt, like the server.
 */
function fakeAttempts(activities: PlayLesson['activities'], resumeWith: AttemptView['questions'] = []) {
  const total = activities.reduce(
    (sum, activity) => sum + activity.questions.length + (activity.matching?.left.length ?? 0),
    0,
  )
  let attempt: AttemptView | null = null
  let number = 0
  const missed = new Set<string>()
  const starts: string[] = []

  const finishIfDone = (current: AttemptView) => {
    const done = current.questions.length + current.matchedPairs.length
    current.doneItems = done
    if (done < total) return
    const correct =
      current.questions.filter((answered) => answered.correct).length +
      current.matchedPairs.filter((pair) => !missed.has(pair.leftId)).length
    const percent = Math.floor((correct * 100) / total)
    current.status = 'Finished'
    current.result = {
      correctItems: correct,
      totalItems: total,
      scorePercent: percent,
      stars: percent >= 100 ? 3 : percent >= 80 ? 2 : percent >= 60 ? 1 : 0,
    }
  }

  const handlers = [
    mockApi('post', '/api/v1/play/lessons/{lessonId}/attempts', ({ params }) => {
      if (!attempt || attempt.status === 'Finished') {
        number += 1
        missed.clear()
        attempt = {
          attemptId: `attempt-${number}`,
          lessonId: String(params.lessonId),
          status: 'InProgress',
          totalItems: total,
          doneItems: 0,
          questions: number === 1 ? [...resumeWith] : [],
          matchedPairs: [],
          result: null,
        }
        finishIfDone(attempt)
      }
      starts.push(attempt.attemptId)
      return structuredClone(attempt)
    }),
    mockApi('post', '/api/v1/play/attempts/{attemptId}/questions/{questionId}', async ({ request, params }) => {
      const { optionId } = (await request.json()) as { optionId: string }
      const questionId = String(params.questionId)
      const activity = activities.find((candidate) =>
        candidate.questions.some((question) => question.id === questionId),
      )!
      const correct = answers[questionId] === optionId
      attempt!.questions.push({
        activityId: activity.id,
        questionId,
        chosenOptionId: optionId,
        correct,
        correctOptionId: answers[questionId]!,
      })
      finishIfDone(attempt!)
      return { correct, correctOptionId: answers[questionId]!, attempt: structuredClone(attempt!) }
    }),
    mockApi('post', '/api/v1/play/attempts/{attemptId}/matches', async ({ request }) => {
      const { leftId, rightId } = (await request.json()) as { leftId: string; rightId: string }
      const correct = answers[leftId] === rightId
      if (correct) {
        attempt!.matchedPairs.push({ activityId: matchingId, leftId, rightId })
      } else {
        missed.add(leftId)
      }
      finishIfDone(attempt!)
      return { correct, correctOptionId: null, attempt: structuredClone(attempt!) }
    }),
  ]
  return { handlers, starts }
}

describe('games', () => {
  it('plays the quiz and the matching one at a time, then shows the stars', async () => {
    const fake = fakeAttempts(games)
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson({ activities: games })),
      ...fake.handlers,
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    // The cards can be skipped; the attempt starts only with the games.
    await user.click(await screen.findByRole('button', { name: 'Xem thẻ luôn' }))
    await user.click(screen.getByRole('button', { name: 'Vào chơi luôn' }))
    expect(fake.starts).toEqual([])
    await user.click(screen.getByRole('button', { name: 'Bắt đầu chơi' }))

    // A wrong answer: the options lock and the right one is marked.
    expect(await screen.findByText('Câu 1 / 2')).toBeTruthy()
    expect(screen.getByRole('img', { name: 'Hình của câu hỏi' }).getAttribute('src')).toBe(
      'https://cdn.example.test/cat.png',
    )
    let options = within(screen.getByRole('group', { name: 'Các đáp án' }))
    await user.click(options.getByRole('button', { name: 'dog' }))
    expect(await screen.findByText('Chưa đúng. Đáp án đúng có dấu tích xanh.')).toBeTruthy()
    expect(options.getByRole('button', { name: 'dog' }).getAttribute('aria-pressed')).toBe('true')
    expect(options.getAllByRole('button').every((button) => button.hasAttribute('disabled'))).toBe(true)

    await user.click(screen.getByRole('button', { name: 'Tiếp' }))
    expect(screen.getByText('Câu 2 / 2')).toBeTruthy()
    expect(screen.getByText('The ___ barks.')).toBeTruthy()
    options = within(screen.getByRole('group', { name: 'Các đáp án' }))
    await user.click(options.getByRole('button', { name: 'dog' }))
    expect(await screen.findByText('Đúng rồi! 🎉')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Tiếp' }))

    // The matching: a miss comes apart, a right match stays.
    expect(screen.getByText('Nối mỗi từ với nghĩa của nó.')).toBeTruthy()
    const words = within(screen.getByRole('group', { name: 'Các từ' }))
    const meanings = within(screen.getByRole('group', { name: 'Các nghĩa' }))
    expect(meanings.getByRole('button', { name: 'con mèo' }).hasAttribute('disabled')).toBe(true)
    await user.click(words.getByRole('button', { name: 'cat' }))
    await user.click(meanings.getByRole('button', { name: 'con chó' }))
    expect(await screen.findByText('Chưa đúng, thử lại nhé!')).toBeTruthy()
    expect(meanings.getByRole('button', { name: 'con chó' }).hasAttribute('disabled')).toBe(true)

    await user.click(words.getByRole('button', { name: 'cat' }))
    await user.click(meanings.getByRole('button', { name: 'con mèo' }))
    await user.click(words.getByRole('button', { name: 'dog' }))
    await user.click(meanings.getByRole('button', { name: 'con chó' }))
    expect(await screen.findByText('Nối đúng hết rồi!')).toBeTruthy()
    expect(words.getByRole('button', { name: 'cat' }).hasAttribute('disabled')).toBe(true)
    await user.click(screen.getByRole('button', { name: 'Tiếp' }))

    // 2 of 4 right (the first question and the missed cat): no stars.
    expect(screen.getByText('Cố lên! Chơi lại nhé!')).toBeTruthy()
    expect(screen.getByRole('img', { name: '0 / 3 sao' })).toBeTruthy()
    expect(screen.getByText('Đúng 2 / 4 (50%)')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Chơi lại' }))
    expect(await screen.findByText('Câu 1 / 2')).toBeTruthy()
    expect(fake.starts).toEqual(['attempt-1', 'attempt-2'])
  })

  it('resumes an unfinished attempt where the child stopped', async () => {
    const fake = fakeAttempts(games, [
      { activityId: quizId, questionId: 'q1', chosenOptionId: 'q1-cat', correct: true, correctOptionId: 'q1-cat' },
    ])
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson({ learnCards: [], activities: games })),
      ...fake.handlers,
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    await user.click(await screen.findByRole('button', { name: 'Bắt đầu chơi' }))

    expect(await screen.findByText('Câu 2 / 2')).toBeTruthy()
  })

  it('fights a boss whose health drops with each right answer', async () => {
    const bossGames: PlayLesson['activities'] = [
      {
        id: quizId,
        type: 'Quiz',
        matching: null,
        questions: [
          {
            id: 'q1',
            form: 'WordToPicture',
            promptText: 'cat',
            promptImageUrl: null,
            options: [
              option('q1-dog', null, 'https://cdn.example.test/dog.png'),
              option('q1-cat', null, 'https://cdn.example.test/cat.png'),
            ],
          },
        ],
      },
    ]
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () =>
        lesson({
          id: bossId,
          title: 'King Lion',
          kind: 'Boss',
          bossImageUrl: 'https://cdn.example.test/lion.png',
          learnCards: [],
          activities: bossGames,
        }),
      ),
      ...fakeAttempts(bossGames).handlers,
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${bossId}` })

    await user.click(await screen.findByRole('button', { name: 'Đánh boss!' }))

    const health = await screen.findByRole('meter', { name: 'Máu của King Lion' })
    expect(health.getAttribute('aria-valuenow')).toBe('1')
    // Picture options never give the word away in their names.
    await user.click(screen.getByRole('button', { name: 'Hình 2' }))
    expect(await screen.findByText('Đúng rồi! 🎉')).toBeTruthy()
    expect(screen.getByRole('meter', { name: 'Máu của King Lion' }).getAttribute('aria-valuenow')).toBe('0')

    await user.click(screen.getByRole('button', { name: 'Tiếp' }))
    expect(screen.getByText('Bạn đã đánh bại King Lion!')).toBeTruthy()
    expect(screen.getByRole('img', { name: '3 / 3 sao' })).toBeTruthy()
  })

  it('offers to start again when the attempt does not start', async () => {
    server.use(
      mockApi('get', '/api/v1/play/lessons/{lessonId}', () => lesson({ learnCards: [], activities: games })),
      mockApi('post', '/api/v1/play/lessons/{lessonId}/attempts', () => problem(500)),
    )
    signInAsChild()
    const { user } = await renderRoutes(routes, { route: `/kids/lessons/${petsId}` })

    await user.click(await screen.findByRole('button', { name: 'Bắt đầu chơi' }))

    expect(await screen.findByText('Chưa vào chơi được. Thử lại nhé!')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Bắt đầu chơi' })).toBeTruthy()
  })
})
