import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { routes } from '@/app/routes'
import type {
  LessonActivities,
  LessonActivity,
  LessonSummary,
  LessonWord,
  LessonWordBank,
  QuizQuestion,
} from '@/areas/admin/content/api'
import { suggestWrongWords } from '@/areas/admin/content/api'
import { contentErrorKey } from '@/areas/admin/content/ContentParts'
import { ApiError } from '@/shared/api/problem-details'
import { Permissions } from '@/shared/auth/permissions'
import { mockApi, problem } from '@/shared/test/api-mocks'
import { renderRoutes } from '@/shared/test/render'
import { server } from '@/shared/test/server'
import { signInAs } from '@/shared/test/session'

const levelId = '00000000-0000-0000-0000-00000000a001'
const lessonId = '00000000-0000-0000-0000-00000000b001'
const catId = '00000000-0000-0000-0000-00000000c001'
const dogId = '00000000-0000-0000-0000-00000000c002'
const catPicture = 'https://cdn.example.test/cat.png'
const route = `/admin/content/lessons/${lessonId}`

function word(
  id: string,
  english: string,
  meaning: string,
  imageUrl: string | null,
  sortOrder: number,
  showInLearnStep = true,
): LessonWord {
  return { id, english, meaning, imageUrl, movement: 'Still', soundUrl: null, sortOrder, showInLearnStep }
}

interface Recorded {
  method: string
  path: string
  search: string
  body?: unknown
}

/**
 * A fake API for one lesson with two words (one without a picture) and the given activities. Records every change.
 * Words and activities share the lesson's version, as in the backend.
 */
function serveLesson(status: 'Draft' | 'Published' = 'Draft', words?: LessonWord[], activities: LessonActivity[] = []) {
  const state = {
    activities,
    lesson: {
      id: lessonId,
      levelId,
      title: 'Pets',
      sortOrder: 1,
      status,
      createdAtUtc: '2026-10-24T08:00:00Z',
      publishedAtUtc: null,
      archivedAtUtc: null,
      version: 'lesson-v1',
      kind: 'Regular',
      bossImageUrl: null,
    } satisfies LessonSummary,
    words: words ?? [word(catId, 'cat', 'con mèo', catPicture, 1), word(dogId, 'dog', 'con chó', null, 2)],
    version: 1,
    requests: [] as Recorded[],
  }
  const bank = (): LessonWordBank => ({
    lessonId,
    lessonStatus: state.lesson.status,
    lessonVersion: `words-v${state.version}`,
    maxWords: 12,
    words: state.words,
  })
  const activityList = (): LessonActivities => ({
    lessonId,
    lessonStatus: state.lesson.status,
    lessonVersion: `words-v${state.version}`,
    maxActivities: 4,
    maxQuestionsPerQuiz: 10,
    activities: state.activities,
  })
  const withQuestions = (activityId: unknown, change: (questions: QuizQuestion[]) => QuizQuestion[]) => {
    state.activities = state.activities.map((activity) =>
      activity.id === activityId ? { ...activity, questions: change(activity.questions) } : activity,
    )
  }
  const record = async (request: Request) => {
    const text = await request.text()
    const url = new URL(request.url)
    state.requests.push({
      method: request.method,
      path: url.pathname,
      search: url.search,
      body: text ? (JSON.parse(text) as unknown) : undefined,
    })
    state.version += 1
  }

  server.use(
    mockApi('get', '/api/v1/admin/content/lessons/{lessonId}', () => state.lesson),
    mockApi('get', '/api/v1/admin/content/lessons/{lessonId}/words', () => bank()),
    mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/words', async ({ request }) => {
      await record(request.clone())
      const body = (await request.json()) as { english: string; meaning: string; imageUrl: string | null }
      state.words = [
        ...state.words,
        word('00000000-0000-0000-0000-00000000c009', body.english, body.meaning, body.imageUrl, 3),
      ]
      return bank()
    }),
    // Before the {wordId} route, which would otherwise take "order" for a word Id.
    mockApi('put', '/api/v1/admin/content/lessons/{lessonId}/words/order', async ({ request }) => {
      await record(request.clone())
      const { wordIds } = (await request.json()) as { wordIds: string[] }
      state.words = wordIds.map((id) => state.words.find((current) => current.id === id)!)
      return bank()
    }),
    mockApi('put', '/api/v1/admin/content/lessons/{lessonId}/words/{wordId}', async ({ request, params }) => {
      await record(request.clone())
      const body = (await request.json()) as { english: string; meaning: string; imageUrl: string | null }
      state.words = state.words.map((current) => (current.id === params.wordId ? { ...current, ...body } : current))
      return bank()
    }),
    mockApi('delete', '/api/v1/admin/content/lessons/{lessonId}/words/{wordId}', async ({ request, params }) => {
      await record(request)
      state.words = state.words.filter((current) => current.id !== params.wordId)
      return bank()
    }),
    mockApi(
      'put',
      '/api/v1/admin/content/lessons/{lessonId}/words/{wordId}/learn-card',
      async ({ request, params }) => {
        await record(request.clone())
        const { show } = (await request.json()) as { show: boolean }
        state.words = state.words.map((current) =>
          current.id === params.wordId ? { ...current, showInLearnStep: show } : current,
        )
        return bank()
      },
    ),
    mockApi('get', '/api/v1/admin/content/lessons/{lessonId}/activities', () => activityList()),
    mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/activities/quiz', async ({ request }) => {
      await record(request)
      const id = `00000000-0000-0000-0000-0000000e000${state.activities.length + 1}`
      state.activities = [
        ...state.activities,
        {
          id,
          type: 'Quiz',
          sortOrder: state.activities.length + 1,
          questions: [],
          matchingMode: null,
          pairWordIds: [],
        },
      ]
      return activityList()
    }),
    mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/activities/matching', async ({ request }) => {
      await record(request.clone())
      const body = (await request.json()) as { mode: string; wordIds: string[] }
      const id = `00000000-0000-0000-0000-0000000e000${state.activities.length + 1}`
      state.activities = [
        ...state.activities,
        {
          id,
          type: 'Matching',
          sortOrder: state.activities.length + 1,
          questions: [],
          matchingMode: body.mode,
          pairWordIds: body.wordIds,
        },
      ]
      return activityList()
    }),
    mockApi(
      'put',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/matching',
      async ({ request, params }) => {
        await record(request.clone())
        const body = (await request.json()) as { mode: string; wordIds: string[] }
        state.activities = state.activities.map((activity) =>
          activity.id === params.activityId
            ? { ...activity, matchingMode: body.mode, pairWordIds: body.wordIds }
            : activity,
        )
        return activityList()
      },
    ),
    mockApi(
      'delete',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}',
      async ({ request, params }) => {
        await record(request)
        state.activities = state.activities.filter((activity) => activity.id !== params.activityId)
        return activityList()
      },
    ),
    mockApi('put', '/api/v1/admin/content/lessons/{lessonId}/activities/order', async ({ request }) => {
      await record(request.clone())
      const { activityIds } = (await request.json()) as { activityIds: string[] }
      state.activities = activityIds.map((id) => state.activities.find((activity) => activity.id === id)!)
      return activityList()
    }),
    mockApi(
      'post',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions',
      async ({ request, params }) => {
        await record(request.clone())
        const body = (await request.json()) as Omit<QuizQuestion, 'id' | 'sortOrder'>
        withQuestions(params.activityId, (questions) => [
          ...questions,
          {
            ...body,
            id: `00000000-0000-0000-0000-0000000f000${questions.length + 1}`,
            sortOrder: questions.length + 1,
          },
        ])
        return activityList()
      },
    ),
    // Before the {questionId} route, which would otherwise take "order" for a question Id.
    mockApi(
      'put',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/order',
      async ({ request, params }) => {
        await record(request.clone())
        const { questionIds } = (await request.json()) as { questionIds: string[] }
        withQuestions(params.activityId, (questions) =>
          questionIds.map((id) => questions.find((question) => question.id === id)!),
        )
        return activityList()
      },
    ),
    mockApi(
      'put',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/{questionId}',
      async ({ request, params }) => {
        await record(request.clone())
        const body = (await request.json()) as Omit<QuizQuestion, 'id' | 'sortOrder'>
        withQuestions(params.activityId, (questions) =>
          questions.map((question) => (question.id === params.questionId ? { ...question, ...body } : question)),
        )
        return activityList()
      },
    ),
    mockApi(
      'delete',
      '/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/{questionId}',
      async ({ request, params }) => {
        await record(request)
        withQuestions(params.activityId, (questions) =>
          questions.filter((question) => question.id !== params.questionId),
        )
        return activityList()
      },
    ),
  )
  return state
}

describe('lesson page', () => {
  it('shows the words in order with their pictures and what is missing before publishing', async () => {
    serveLesson()
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByRole('heading', { name: 'Pets' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Từ vựng (2/12)' })).toBeTruthy()
    const rows = screen.getAllByRole('row')
    expect(within(rows[1]!).getByText('cat')).toBeTruthy()
    expect(within(rows[1]!).getByText('con mèo')).toBeTruthy()
    expect(within(rows[1]!).getByRole('img', { name: 'cat' }).getAttribute('src')).toBe(catPicture)
    expect(within(rows[2]!).getByRole('img', { name: 'Chưa có hình' })).toBeTruthy()
    expect(screen.getByText(/Còn 1 từ chưa có hình/)).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Quay lại level' }).getAttribute('href')).toBe(
      `/admin/content/levels/${levelId}`,
    )
  })

  it('adds a word with the lesson version, trimmed and without a picture', async () => {
    const state = serveLesson()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm từ' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Từ tiếng Anh'), '  fish ')
    await user.type(within(dialog).getByLabelText('Nghĩa tiếng Việt'), 'con cá')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    expect(await screen.findByText('fish')).toBeTruthy()
    expect(state.requests).toEqual([
      {
        method: 'POST',
        path: `/api/v1/admin/content/lessons/${lessonId}/words`,
        search: '',
        body: {
          english: 'fish',
          meaning: 'con cá',
          imageUrl: null,
          movement: 'Still',
          soundUrl: null,
          version: 'words-v1',
        },
      },
    ])
  })

  it('accepts only https picture addresses and shows a preview', async () => {
    const state = serveLesson()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm từ' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Từ tiếng Anh'), 'fish')
    await user.type(within(dialog).getByLabelText('Nghĩa tiếng Việt'), 'con cá')
    await user.type(within(dialog).getByLabelText('Địa chỉ hình (https://)'), 'http://cdn.example.test/fish.png')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    expect(await within(dialog).findByText(/phải bắt đầu bằng https:\/\//)).toBeTruthy()
    expect(state.requests).toEqual([])

    await user.clear(within(dialog).getByLabelText('Địa chỉ hình (https://)'))
    await user.type(within(dialog).getByLabelText('Địa chỉ hình (https://)'), 'https://cdn.example.test/fish.png')
    expect(within(dialog).getByRole('img', { name: 'Xem trước hình' }).getAttribute('src')).toBe(
      'https://cdn.example.test/fish.png',
    )
  })

  it('sets how a word moves and sounds on the discovery walk', async () => {
    const state = serveLesson()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Sửa từ dog' }))
    const dialog = await screen.findByRole('dialog')
    const movement = within(dialog).getByLabelText('Cách di chuyển khi khám phá')
    expect((movement as HTMLSelectElement).value).toBe('Still')
    expect(within(dialog).getByRole('button', { name: 'Nghe thử' }).hasAttribute('disabled')).toBe(true)
    await user.selectOptions(movement, 'Walk')
    const sound = within(dialog).getByLabelText('Địa chỉ tiếng kêu (https://, không bắt buộc)')
    await user.type(sound, 'http://cdn.example.test/woof.mp3')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await within(dialog).findByText(/Địa chỉ tiếng kêu phải bắt đầu bằng https:\/\//)).toBeTruthy()
    expect(state.requests).toEqual([])

    await user.clear(sound)
    await user.type(sound, 'https://cdn.example.test/woof.mp3')
    expect(within(dialog).getByRole('button', { name: 'Nghe thử' }).hasAttribute('disabled')).toBe(false)
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]!.body).toMatchObject({ movement: 'Walk', soundUrl: 'https://cdn.example.test/woof.mp3' })
    const row = (await screen.findByRole('cell', { name: /Đi bộ/ })).closest('tr')!
    expect(within(row).getByText('dog')).toBeTruthy()
    expect(within(row).getByText('có tiếng')).toBeTruthy()
  })

  it('marks a duplicate word on the field', async () => {
    serveLesson()
    server.use(
      mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/words', () =>
        problem(422, { title: 'Domain rule violation.', code: 'word-duplicate' }),
      ),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm từ' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Từ tiếng Anh'), 'Cat')
    await user.type(within(dialog).getByLabelText('Nghĩa tiếng Việt'), 'mèo')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    expect(await within(dialog).findByText('Bài đã có từ này.')).toBeTruthy()
  })

  it('corrects, reorders and removes words with the lesson version', async () => {
    const state = serveLesson()
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Sửa từ dog' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Địa chỉ hình (https://)'), 'https://cdn.example.test/dog.png')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())

    await user.click(await screen.findByRole('button', { name: 'Chuyển dog lên' }))
    await waitFor(() => expect(state.requests).toHaveLength(2))

    await user.click(screen.getByRole('button', { name: 'Xoá từ cat' }))
    const confirm = await screen.findByRole('alertdialog')
    await user.click(within(confirm).getByRole('button', { name: 'Xoá từ' }))

    await waitFor(() => expect(state.requests).toHaveLength(3))
    expect(state.requests).toEqual([
      {
        method: 'PUT',
        path: `/api/v1/admin/content/lessons/${lessonId}/words/${dogId}`,
        search: '',
        body: {
          english: 'dog',
          meaning: 'con chó',
          imageUrl: 'https://cdn.example.test/dog.png',
          movement: 'Still',
          soundUrl: null,
          version: 'words-v1',
        },
      },
      {
        method: 'PUT',
        path: `/api/v1/admin/content/lessons/${lessonId}/words/order`,
        search: '',
        body: { wordIds: [dogId, catId], version: 'words-v2' },
      },
      {
        method: 'DELETE',
        path: `/api/v1/admin/content/lessons/${lessonId}/words/${catId}`,
        search: '?version=words-v3',
      },
    ])
    await waitFor(() => expect(screen.queryByText('cat')).toBeNull())
  })

  it('locks the structure of a published lesson and keeps its pictures', async () => {
    const state = serveLesson('Published', [word(catId, 'cat', 'con mèo', catPicture, 1)])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    expect(await screen.findByText(/chỉ sửa được chữ và hình của từ/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Thêm từ' })).toBeNull()
    expect(screen.queryByRole('button', { name: /Xoá từ/ })).toBeNull()
    expect(screen.queryByRole('button', { name: /Chuyển/ })).toBeNull()

    await user.click(screen.getByRole('button', { name: 'Sửa từ cat' }))
    const dialog = await screen.findByRole('dialog')
    await user.clear(within(dialog).getByLabelText('Địa chỉ hình (https://)'))
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await within(dialog).findByText('Từ của bài đã xuất bản phải có hình.')).toBeTruthy()
    expect(state.requests).toEqual([])
  })

  it('explains why a lesson cannot be published yet (lesson-not-ready)', async () => {
    serveLesson()
    server.use(
      mockApi('post', '/api/v1/admin/content/lessons/{lessonId}/publish', () =>
        problem(422, { title: 'Domain rule violation.', code: 'lesson-not-ready' }),
      ),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Xuất bản' }))

    expect(await screen.findByText(/Bài chưa thể xuất bản/)).toBeTruthy()
  })

  it('disables adding when the lesson has all its words', async () => {
    serveLesson(
      'Draft',
      Array.from({ length: 12 }, (_, index) =>
        word(
          `00000000-0000-0000-0000-0000000d${String(index).padStart(4, '0')}`,
          `word ${index}`,
          'nghĩa',
          catPicture,
          index + 1,
        ),
      ),
    )
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect((await screen.findByRole('button', { name: 'Thêm từ' })).hasAttribute('disabled')).toBe(true)
    expect(screen.getByText('Bài đã có đủ 12 từ.')).toBeTruthy()
  })

  it('shows the words read-only without content.manage', async () => {
    serveLesson()
    signInAs('ContentAdmin', { permissions: [Permissions.contentReadDrafts] })
    await renderRoutes(routes, { route })

    expect(await screen.findByText('cat')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Thêm từ' })).toBeNull()
    expect(screen.queryByRole('button', { name: /Sửa từ/ })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Xuất bản' })).toBeNull()
  })

  it('shows not found for an unknown lesson', async () => {
    server.use(
      mockApi('get', '/api/v1/admin/content/lessons/{lessonId}', () => problem(404)),
      mockApi('get', '/api/v1/admin/content/lessons/{lessonId}/words', () => problem(404)),
    )
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByText('Không tìm thấy bài học này.')).toBeTruthy()
  })
})

describe('learn step cards', () => {
  const pictured = () => [word(catId, 'cat', 'con mèo', catPicture, 1), word(dogId, 'dog', 'con chó', catPicture, 2)]

  it('switches a word out of the learn step with the lesson version', async () => {
    const state = serveLesson('Draft', pictured())
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    expect(await screen.findByText(/Bước Làm quen: 2 thẻ/)).toBeTruthy()
    const dogCard = screen.getByRole('checkbox', { name: 'Hiện dog trong bước Làm quen' })
    expect(dogCard.getAttribute('aria-checked')).toBe('true')
    await user.click(dogCard)

    await waitFor(() =>
      expect(state.requests).toEqual([
        {
          method: 'PUT',
          path: `/api/v1/admin/content/lessons/${lessonId}/words/${dogId}/learn-card`,
          search: '',
          body: { show: false, version: 'words-v1' },
        },
      ]),
    )
    expect(await screen.findByText(/Bước Làm quen: 1 thẻ/)).toBeTruthy()
    await waitFor(() =>
      expect(screen.getByRole('checkbox', { name: 'Hiện dog trong bước Làm quen' }).getAttribute('aria-checked')).toBe(
        'false',
      ),
    )
  })

  it('says a draft needs a learn card when every word is left out', async () => {
    serveLesson('Draft', [word(catId, 'cat', 'con mèo', catPicture, 1, false)])
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByText(/Bước Làm quen chưa có thẻ nào/)).toBeTruthy()
  })

  it('shows the cards without switches in a published lesson', async () => {
    serveLesson('Published', [
      word(catId, 'cat', 'con mèo', catPicture, 1),
      word(dogId, 'dog', 'con chó', catPicture, 2, false),
    ])
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByText('cat')).toBeTruthy()
    expect(screen.queryByRole('checkbox')).toBeNull()
    const rows = screen.getAllByRole('row')
    expect(within(rows[1]!).getByText('Có')).toBeTruthy()
    expect(within(rows[2]!).getByText('Không')).toBeTruthy()
  })
})

describe('quiz editor', () => {
  const fishId = '00000000-0000-0000-0000-00000000c003'
  const threeWords = () => [
    word(catId, 'cat', 'con mèo', catPicture, 1),
    word(dogId, 'dog', 'con chó', catPicture, 2),
    word(fishId, 'fish', 'con cá', catPicture, 3),
  ]
  const quizId = '00000000-0000-0000-0000-0000000e0001'
  const question = (id: string, overrides: Partial<QuizQuestion> = {}): QuizQuestion => ({
    id,
    sortOrder: 1,
    form: 'PictureToWord',
    correctWordId: catId,
    wrongWordIds: [dogId],
    sentence: null,
    ...overrides,
  })
  const quiz = (questions: QuizQuestion[] = []): LessonActivity => ({
    id: quizId,
    type: 'Quiz',
    sortOrder: 1,
    questions,
    matchingMode: null,
    pairWordIds: [],
  })

  it('adds a quiz and a question with suggested wrong words', async () => {
    const state = serveLesson('Draft', threeWords())
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    expect(await screen.findByText(/Bài chưa có hoạt động nào/)).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Thêm quiz' }))
    await user.click(await screen.findByRole('button', { name: 'Thêm câu hỏi vào Quiz 1' }))
    const dialog = await screen.findByRole('dialog')
    // The words after the correct one are suggested.
    expect(within(dialog).getByRole('checkbox', { name: 'dog' }).getAttribute('aria-checked')).toBe('true')
    expect(within(dialog).getByRole('checkbox', { name: 'fish' }).getAttribute('aria-checked')).toBe('true')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    await waitFor(() => expect(state.requests).toHaveLength(2))
    expect(state.requests).toEqual([
      {
        method: 'POST',
        path: `/api/v1/admin/content/lessons/${lessonId}/activities/quiz`,
        search: '',
        body: { version: 'words-v1' },
      },
      {
        method: 'POST',
        path: `/api/v1/admin/content/lessons/${lessonId}/activities/${quizId}/questions`,
        search: '',
        body: {
          form: 'PictureToWord',
          correctWordId: catId,
          wrongWordIds: [dogId, fishId],
          sentence: null,
          version: 'words-v2',
        },
      },
    ])
    expect(await screen.findByText('1/10 câu hỏi')).toBeTruthy()
    expect(screen.queryByText(/Bài chưa có hoạt động nào/)).toBeNull()
  })

  it('suggests again when the correct word changes, and lets the Admin choose', async () => {
    const state = serveLesson('Draft', threeWords(), [quiz()])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    expect(await screen.findByText(/Có quiz chưa có câu hỏi nào/)).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Thêm câu hỏi vào Quiz 1' }))
    const dialog = await screen.findByRole('dialog')
    await user.selectOptions(within(dialog).getByLabelText('Từ đúng'), dogId)
    expect(within(dialog).queryByRole('checkbox', { name: 'dog' })).toBeNull()
    await user.click(within(dialog).getByRole('checkbox', { name: 'cat' }))
    await user.click(within(dialog).getByRole('radio', { name: 'Đọc từ chọn hình' }))
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]!.body).toEqual({
      form: 'WordToPicture',
      correctWordId: dogId,
      wrongWordIds: [fishId],
      sentence: null,
      version: 'words-v1',
    })
  })

  it('checks a fill-in sentence for exactly one gap', async () => {
    const state = serveLesson('Draft', threeWords(), [quiz()])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm câu hỏi vào Quiz 1' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('radio', { name: 'Điền từ vào câu' }))
    await user.type(within(dialog).getByLabelText('Câu có chỗ trống'), 'I have a cat.')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))
    expect(await within(dialog).findByText(/đúng một chỗ trống/)).toBeTruthy()
    expect(state.requests).toEqual([])

    await user.clear(within(dialog).getByLabelText('Câu có chỗ trống'))
    await user.type(within(dialog).getByLabelText('Câu có chỗ trống'), 'I have a ___.')
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]!.body).toMatchObject({ form: 'FillInTheBlank', sentence: 'I have a ___.' })
  })

  it('shows the questions, reorders and removes them, and removes a quiz after confirming', async () => {
    const first = '00000000-0000-0000-0000-0000000f0001'
    const second = '00000000-0000-0000-0000-0000000f0002'
    const state = serveLesson('Draft', threeWords(), [
      quiz([
        question(first),
        question(second, {
          sortOrder: 2,
          form: 'FillInTheBlank',
          correctWordId: fishId,
          wrongWordIds: [catId, dogId],
          sentence: 'A ___ swims.',
        }),
      ]),
    ])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    const card = await screen.findByRole('article', { name: 'Quiz 1' })
    expect(within(card).getByText('Nhìn hình chọn từ')).toBeTruthy()
    expect(within(card).getByText('A ___ swims.')).toBeTruthy()
    expect(within(card).getByText('cat, dog')).toBeTruthy()

    await user.click(within(card).getByRole('button', { name: 'Chuyển câu 2 lên' }))
    await waitFor(() => expect(state.requests).toHaveLength(1))
    // Wait for the new order on screen: "question 2" is now the picture question.
    await waitFor(() => expect(within(within(card).getAllByRole('row')[1]!).getByText('A ___ swims.')).toBeTruthy())
    await user.click(within(card).getByRole('button', { name: 'Xoá câu 2' }))
    await waitFor(() => expect(state.requests).toHaveLength(2))
    await user.click(within(card).getByRole('button', { name: 'Xoá Quiz 1' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Xoá quiz' }))

    await waitFor(() => expect(state.requests).toHaveLength(3))
    expect(state.requests.map((request) => `${request.method} ${request.path}${request.search}`)).toEqual([
      `PUT /api/v1/admin/content/lessons/${lessonId}/activities/${quizId}/questions/order`,
      `DELETE /api/v1/admin/content/lessons/${lessonId}/activities/${quizId}/questions/${first}?version=words-v2`,
      `DELETE /api/v1/admin/content/lessons/${lessonId}/activities/${quizId}?version=words-v3`,
    ])
    expect(state.requests[0]!.body).toEqual({ questionIds: [second, first], version: 'words-v1' })
    expect(await screen.findByText('Bài này chưa có hoạt động nào.')).toBeTruthy()
  })

  it('corrects only fill-in sentences in a published lesson', async () => {
    const fill = '00000000-0000-0000-0000-0000000f0002'
    const state = serveLesson('Published', threeWords(), [
      quiz([
        question('00000000-0000-0000-0000-0000000f0001'),
        question(fill, { sortOrder: 2, form: 'FillInTheBlank', sentence: 'I has a ___.' }),
      ]),
    ])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    const card = await screen.findByRole('article', { name: 'Quiz 1' })
    expect(screen.queryByRole('button', { name: 'Thêm quiz' })).toBeNull()
    expect(within(card).queryByRole('button', { name: /Thêm câu hỏi/ })).toBeNull()
    expect(within(card).queryByRole('button', { name: 'Sửa câu 1' })).toBeNull()
    expect(within(card).queryByRole('button', { name: /Xoá/ })).toBeNull()

    await user.click(within(card).getByRole('button', { name: 'Sửa câu 2' }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText(/chỉ sửa được câu điền từ/)).toBeTruthy()
    await user.clear(within(dialog).getByLabelText('Câu có chỗ trống'))
    await user.type(within(dialog).getByLabelText('Câu có chỗ trống'), 'I have a ___.')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]!.body).toEqual({
      form: 'FillInTheBlank',
      correctWordId: catId,
      wrongWordIds: [dogId],
      sentence: 'I have a ___.',
      version: 'words-v1',
    })
  })

  it('needs two words before a quiz can be added', async () => {
    serveLesson('Draft', [word(catId, 'cat', 'con mèo', catPicture, 1)])
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect((await screen.findByRole('button', { name: 'Thêm quiz' })).hasAttribute('disabled')).toBe(true)
    expect(screen.getByRole('button', { name: 'Thêm matching' }).hasAttribute('disabled')).toBe(true)
    expect(screen.getByText('Cần ít nhất 2 từ trong ngân hàng từ vựng để tạo quiz hoặc matching.')).toBeTruthy()
  })

  it('explains that a word used by a question cannot be removed', async () => {
    serveLesson('Draft', threeWords(), [quiz([question('00000000-0000-0000-0000-0000000f0001')])])
    server.use(
      mockApi('delete', '/api/v1/admin/content/lessons/{lessonId}/words/{wordId}', () =>
        problem(422, { title: 'Domain rule violation.', code: 'word-in-use' }),
      ),
    )
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Xoá từ dog' }))
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Xoá từ' }))

    expect(await screen.findByText(/Một câu hỏi quiz đang dùng từ này/)).toBeTruthy()
  })
})

describe('matching editor', () => {
  const fishId = '00000000-0000-0000-0000-00000000c003'
  const words = () => [
    word(catId, 'cat', 'con mèo', catPicture, 1),
    word(dogId, 'dog', 'con chó', catPicture, 2),
    word(fishId, 'fish', 'con cá', catPicture, 3),
  ]
  const matchingId = '00000000-0000-0000-0000-0000000e0001'
  const matching = (mode = 'WordToPicture', pairWordIds = [catId, dogId]): LessonActivity => ({
    id: matchingId,
    type: 'Matching',
    sortOrder: 1,
    questions: [],
    matchingMode: mode,
    pairWordIds,
  })

  it('adds a matching of the chosen words in the word bank order', async () => {
    const state = serveLesson('Draft', words())
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm matching' }))
    const dialog = await screen.findByRole('dialog')
    // By default the first words are chosen, matched with pictures.
    expect(within(dialog).getByRole('radio', { name: 'Nối từ với hình' }).getAttribute('aria-checked')).toBe('true')
    expect(within(dialog).getByRole('checkbox', { name: 'fish' }).getAttribute('aria-checked')).toBe('true')
    await user.click(within(dialog).getByRole('checkbox', { name: 'cat' }))
    await user.click(within(dialog).getByRole('radio', { name: 'Nối từ với nghĩa' }))
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]).toEqual({
      method: 'POST',
      path: `/api/v1/admin/content/lessons/${lessonId}/activities/matching`,
      search: '',
      body: { mode: 'WordToMeaning', wordIds: [dogId, fishId], version: 'words-v1' },
    })
    const card = await screen.findByRole('article', { name: 'Matching 1' })
    expect(within(card).getByText('con chó')).toBeTruthy()
    expect(within(card).getByText(/2 cặp/)).toBeTruthy()
  })

  it('needs at least two words', async () => {
    const state = serveLesson('Draft', words())
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Thêm matching' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('checkbox', { name: 'cat' }))
    await user.click(within(dialog).getByRole('checkbox', { name: 'dog' }))
    await user.click(within(dialog).getByRole('button', { name: 'Thêm' }))

    expect(await within(dialog).findByText('Hãy chọn ít nhất 2 từ.')).toBeTruthy()
    expect(state.requests).toEqual([])
  })

  it('shows a matching with pictures and changes it in a draft', async () => {
    const state = serveLesson('Draft', words(), [matching()])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    const card = await screen.findByRole('article', { name: 'Matching 1' })
    expect(within(card).getByText(/Nối từ với hình/)).toBeTruthy()
    expect(within(card).getAllByRole('img', { name: 'cat' })).toHaveLength(1)
    await user.click(within(card).getByRole('button', { name: 'Sửa Matching 1' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('checkbox', { name: 'fish' }))
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() => expect(state.requests).toHaveLength(1))
    expect(state.requests[0]).toEqual({
      method: 'PUT',
      path: `/api/v1/admin/content/lessons/${lessonId}/activities/${matchingId}/matching`,
      search: '',
      body: { mode: 'WordToPicture', wordIds: [catId, dogId, fishId], version: 'words-v1' },
    })
  })

  it('locks a matching of a published lesson', async () => {
    serveLesson('Published', words(), [matching('WordToMeaning')])
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    const card = await screen.findByRole('article', { name: 'Matching 1' })
    expect(within(card).getByText('con mèo')).toBeTruthy()
    expect(within(card).queryByRole('button')).toBeNull()
    expect(screen.queryByRole('button', { name: 'Thêm matching' })).toBeNull()
  })
})

describe('boss page', () => {
  const lionPicture = 'https://cdn.example.test/lion.png'

  /** The fake lesson as the level's boss: the words are the level's, and it has no word bank of its own. */
  function serveBoss(bossImageUrl: string | null, words: LessonWord[], activities: LessonActivity[] = []) {
    const state = serveLesson('Draft', words, activities)
    Object.assign(state.lesson, { kind: 'Boss', title: 'King Lion', bossImageUrl })
    server.use(
      mockApi('put', '/api/v1/admin/content/lessons/{lessonId}/boss', async ({ request }) => {
        const body = (await request.json()) as { title: string; imageUrl: string | null; version: string }
        state.requests.push({ method: 'PUT', path: new URL(request.url).pathname, search: '', body })
        Object.assign(state.lesson, { title: body.title, bossImageUrl: body.imageUrl })
        return state.lesson
      }),
    )
    return state
  }

  it('shows the boss without a word bank and asks for its picture first', async () => {
    serveBoss(null, [word(catId, 'cat', 'con mèo', catPicture, 1), word(dogId, 'dog', 'con chó', catPicture, 2)])
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByRole('heading', { name: 'King Lion' })).toBeTruthy()
    expect(screen.getByText(/chọn trong 2 từ của các bài trong level/)).toBeTruthy()
    expect(screen.queryByRole('heading', { name: /Từ vựng/ })).toBeNull()
    expect(screen.getByText('Boss cần có hình trước khi xuất bản.')).toBeTruthy()
    // Its activities choose from the level's words.
    expect(screen.getByRole('button', { name: 'Thêm matching' }).hasAttribute('disabled')).toBe(false)
  })

  it('names the words the boss uses that still need a picture', async () => {
    serveBoss(
      lionPicture,
      [word(catId, 'cat', 'con mèo', catPicture, 1), word(dogId, 'dog', 'con chó', null, 2)],
      [
        {
          id: '00000000-0000-0000-0000-0000000e0001',
          type: 'Matching',
          sortOrder: 1,
          questions: [],
          matchingMode: 'WordToMeaning',
          pairWordIds: [catId, dogId],
        },
      ],
    )
    signInAs('ContentAdmin')
    await renderRoutes(routes, { route })

    expect(await screen.findByText(/Còn 1 từ boss dùng chưa có hình/)).toBeTruthy()
  })

  it('changes the boss name and picture with its version', async () => {
    const state = serveBoss(null, [word(catId, 'cat', 'con mèo', catPicture, 1)])
    signInAs('ContentAdmin')
    const { user } = await renderRoutes(routes, { route })

    await user.click(await screen.findByRole('button', { name: 'Sửa tên và hình boss' }))
    const dialog = await screen.findByRole('dialog')
    await user.clear(within(dialog).getByLabelText('Tên boss'))
    await user.type(within(dialog).getByLabelText('Tên boss'), 'Lion King')
    await user.type(within(dialog).getByLabelText('Địa chỉ hình boss (https://)'), lionPicture)
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() =>
      expect(state.requests).toEqual([
        {
          method: 'PUT',
          path: `/api/v1/admin/content/lessons/${lessonId}/boss`,
          search: '',
          body: { title: 'Lion King', imageUrl: lionPicture, version: 'lesson-v1' },
        },
      ]),
    )
    expect(await screen.findByRole('heading', { name: 'Lion King' })).toBeTruthy()
  })
})

describe('suggestWrongWords', () => {
  const words = [{ id: 'a' }, { id: 'b' }, { id: 'c' }, { id: 'd' }]

  it('takes the words after the correct one, wrapping around', () => {
    expect(suggestWrongWords(words, 'a', 2)).toEqual(['b', 'c'])
    expect(suggestWrongWords(words, 'd', 2)).toEqual(['a', 'b'])
    expect(suggestWrongWords(words, 'b', 5)).toEqual(['c', 'd', 'a'])
    expect(suggestWrongWords(words, 'x', 2)).toEqual([])
  })
})

describe('contentErrorKey', () => {
  it('uses the rule code of a 422 and falls back without one', () => {
    const rule = (code?: string) => new ApiError('rule', 422, { title: 'x', code })

    expect(contentErrorKey(rule('lesson-structure-locked'))).toBe('admin.content.errors.structureLocked')
    expect(contentErrorKey(rule('content-started'), 'publish')).toBe('admin.content.errors.started')
    expect(contentErrorKey(rule(), 'unpublish')).toBe('admin.content.errors.started')
    expect(contentErrorKey(rule('something-new'))).toBe('admin.content.errors.archived')
  })
})
