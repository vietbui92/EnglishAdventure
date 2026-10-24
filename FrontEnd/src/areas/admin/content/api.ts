import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type LevelSummary = components['schemas']['LevelSummary']
export type LevelDetails = components['schemas']['LevelDetails']
export type LessonSummary = components['schemas']['LessonSummary']
export type LessonWordBank = components['schemas']['LessonWordBank']
export type LessonWord = components['schemas']['LessonWordSummary']
export type LessonActivities = components['schemas']['LessonActivities']
export type LessonActivity = components['schemas']['ActivitySummary']
export type QuizQuestion = components['schemas']['QuizQuestionSummary']

/** The quiz question forms of the backend (Domain/Enums/QuizQuestionForm.cs). */
export const quizForms = ['PictureToWord', 'WordToPicture', 'FillInTheBlank'] as const
export type QuizForm = (typeof quizForms)[number]

/** The matching modes of the backend (Domain/Enums/MatchingMode.cs); word ↔ picture is the default. */
export const matchingModes = ['WordToPicture', 'WordToMeaning'] as const
export type MatchingMode = (typeof matchingModes)[number]

export type ContentStatus = 'Draft' | 'Published' | 'Archived'

/** Publish, unpublish and archive need content.publish; archiving is final (PD-012). */
export type ContentStatusAction = 'publish' | 'unpublish' | 'archive'

export const contentKeys = {
  all: ['admin', 'content'] as const,
  levels: (includeArchived: boolean) => [...contentKeys.all, 'levels', { includeArchived }] as const,
  level: (levelId: string) => [...contentKeys.all, 'level', levelId] as const,
  lesson: (lessonId: string) => [...contentKeys.all, 'lesson', lessonId] as const,
  words: (lessonId: string) => [...contentKeys.all, 'words', lessonId] as const,
  activities: (lessonId: string) => [...contentKeys.all, 'activities', lessonId] as const,
}

/** GET /api/v1/admin/content/levels (content.read-drafts). Non-archived levels in their order, then archived ones. */
export function useLevels(includeArchived: boolean) {
  return useQuery({
    queryKey: contentKeys.levels(includeArchived),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/admin/content/levels', { params: { query: { includeArchived } }, signal })),
  })
}

/** GET /api/v1/admin/content/levels/{levelId} (content.read-drafts): the level with all its lessons. */
export function useLevel(levelId: string) {
  return useQuery({
    queryKey: contentKeys.level(levelId),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/admin/content/levels/{levelId}', { params: { path: { levelId } }, signal })),
  })
}

// Every change reloads the content views, also after a failure: a 409 or 422 means the page shows stale data.
function useContentMutation<Input, Output>(mutationFn: (input: Input) => Promise<Output>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSettled: () => queryClient.invalidateQueries({ queryKey: contentKeys.all }),
  })
}

export interface LevelInput {
  title: string
  description: string
}

/** POST /api/v1/admin/content/levels (content.manage): a draft level at the end. Resolves to the new level. */
export function useCreateLevel() {
  return useContentMutation((input: LevelInput) => unwrap(api.POST('/api/v1/admin/content/levels', { body: input })))
}

/** PUT /api/v1/admin/content/levels/{levelId} (content.manage). 409 when the version is stale. */
export function useUpdateLevel(levelId: string) {
  return useContentMutation((input: LevelInput & { version: string }) =>
    unwrap(api.PUT('/api/v1/admin/content/levels/{levelId}', { params: { path: { levelId } }, body: input })),
  )
}

/** PUT /api/v1/admin/content/levels/order (content.manage) with every non-archived level once. */
export function useReorderLevels() {
  return useContentMutation((levelIds: string[]) =>
    unwrap(api.PUT('/api/v1/admin/content/levels/order', { body: { levelIds } })),
  )
}

export interface StatusChange {
  action: ContentStatusAction
  version: string
}

/** POST .../publish, .../unpublish or .../archive of a level (content.publish). */
export function useLevelStatus(levelId: string) {
  return useContentMutation(({ action, version }: StatusChange) => {
    const options = { params: { path: { levelId } }, body: { version } }
    switch (action) {
      case 'publish':
        return unwrap(api.POST('/api/v1/admin/content/levels/{levelId}/publish', options))
      case 'unpublish':
        return unwrap(api.POST('/api/v1/admin/content/levels/{levelId}/unpublish', options))
      case 'archive':
        return unwrap(api.POST('/api/v1/admin/content/levels/{levelId}/archive', options))
    }
  })
}

/** POST /api/v1/admin/content/levels/{levelId}/lessons (content.manage): a draft lesson at the end of the level. */
export function useCreateLesson(levelId: string) {
  return useContentMutation((input: { title: string }) =>
    unwrap(api.POST('/api/v1/admin/content/levels/{levelId}/lessons', { params: { path: { levelId } }, body: input })),
  )
}

/** A boss challenge is a lesson of kind "Boss" (S3.8): it comes after the lessons and is not in their order. */
export function isBoss(lesson: { kind: string }) {
  return lesson.kind === 'Boss'
}

export interface BossInput {
  /** The boss's name. */
  title: string
  /** Blank for no picture yet; required before the boss is published. */
  imageUrl: string
}

/** POST .../levels/{levelId}/boss (content.manage): the level's boss as a draft. 422 boss-exists for a second one. */
export function useCreateBoss(levelId: string) {
  return useContentMutation((input: BossInput) =>
    unwrap(
      api.POST('/api/v1/admin/content/levels/{levelId}/boss', {
        params: { path: { levelId } },
        body: { title: input.title, imageUrl: input.imageUrl || null },
      }),
    ),
  )
}

/** PUT .../lessons/{lessonId}/boss (content.manage): the boss's name and picture, also while published. */
export function useUpdateBoss(lessonId: string) {
  return useContentMutation((input: BossInput & { version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/boss', {
        params: { path: { lessonId } },
        body: { title: input.title, imageUrl: input.imageUrl || null, version: input.version },
      }),
    ),
  )
}

/** PUT /api/v1/admin/content/levels/{levelId}/lessons/order (content.manage) with every non-archived lesson once. */
export function useReorderLessons(levelId: string) {
  return useContentMutation((lessonIds: string[]) =>
    unwrap(
      api.PUT('/api/v1/admin/content/levels/{levelId}/lessons/order', {
        params: { path: { levelId } },
        body: { lessonIds },
      }),
    ),
  )
}

/** PUT /api/v1/admin/content/lessons/{lessonId} (content.manage). 409 when the version is stale. */
export function useUpdateLesson(lessonId: string) {
  return useContentMutation((input: { title: string; version: string }) =>
    unwrap(api.PUT('/api/v1/admin/content/lessons/{lessonId}', { params: { path: { lessonId } }, body: input })),
  )
}

/** POST .../publish, .../unpublish or .../archive of a lesson (content.publish). */
export function useLessonStatus(lessonId: string) {
  return useContentMutation(({ action, version }: StatusChange) => {
    const options = { params: { path: { lessonId } }, body: { version } }
    switch (action) {
      case 'publish':
        return unwrap(api.POST('/api/v1/admin/content/lessons/{lessonId}/publish', options))
      case 'unpublish':
        return unwrap(api.POST('/api/v1/admin/content/lessons/{lessonId}/unpublish', options))
      case 'archive':
        return unwrap(api.POST('/api/v1/admin/content/lessons/{lessonId}/archive', options))
    }
  })
}

/** GET /api/v1/admin/content/lessons/{lessonId} (content.read-drafts). */
export function useLesson(lessonId: string) {
  return useQuery({
    queryKey: contentKeys.lesson(lessonId),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/admin/content/lessons/{lessonId}', { params: { path: { lessonId } }, signal })),
  })
}

/** GET .../lessons/{lessonId}/words (content.read-drafts): the word bank with the lesson's status and version (S3.6). */
export function useLessonWords(lessonId: string) {
  return useQuery({
    queryKey: contentKeys.words(lessonId),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/admin/content/lessons/{lessonId}/words', { params: { path: { lessonId } }, signal })),
  })
}

export interface WordInput {
  english: string
  meaning: string
  /** Blank for no picture yet; every word needs one before the lesson is published. */
  imageUrl: string
  /** How the picture moves on the discovery walk (S3.9, PD-013). */
  movement: WordMovement
  /** The sound the word makes; blank for none. */
  soundUrl: string
}

/** The movements of the backend's WordMovement, in the order the Admin sees them. */
export const wordMovements = ['Still', 'Walk', 'Hop', 'Fly', 'Swim', 'Crawl', 'Wiggle'] as const
export type WordMovement = (typeof wordMovements)[number]

/** Word changes send the lesson's version: it covers the whole word bank (409 when someone else changed it). */
export function useAddWord(lessonId: string) {
  return useContentMutation((input: WordInput & { version: string }) =>
    unwrap(
      api.POST('/api/v1/admin/content/lessons/{lessonId}/words', {
        params: { path: { lessonId } },
        body: { ...input, imageUrl: input.imageUrl || null, soundUrl: input.soundUrl || null },
      }),
    ),
  )
}

export function useUpdateWord(lessonId: string) {
  return useContentMutation(({ wordId, ...input }: WordInput & { wordId: string; version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/words/{wordId}', {
        params: { path: { lessonId, wordId } },
        body: { ...input, imageUrl: input.imageUrl || null, soundUrl: input.soundUrl || null },
      }),
    ),
  )
}

export function useRemoveWord(lessonId: string) {
  return useContentMutation(({ wordId, version }: { wordId: string; version: string }) =>
    unwrap(
      api.DELETE('/api/v1/admin/content/lessons/{lessonId}/words/{wordId}', {
        params: { path: { lessonId, wordId }, query: { version } },
      }),
    ),
  )
}

export function useReorderWords(lessonId: string) {
  return useContentMutation(({ wordIds, version }: { wordIds: string[]; version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/words/order', {
        params: { path: { lessonId } },
        body: { wordIds, version },
      }),
    ),
  )
}

/** PUT .../words/{wordId}/learn-card (content.manage): shows or hides the word in the learn step, drafts only (S3.7). */
export function useSetLearnCard(lessonId: string) {
  return useContentMutation(({ wordId, show, version }: { wordId: string; show: boolean; version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/words/{wordId}/learn-card', {
        params: { path: { lessonId, wordId } },
        body: { show, version },
      }),
    ),
  )
}

/** GET .../lessons/{lessonId}/activities (content.read-drafts): quizzes with their questions and answers (S3.2). */
export function useLessonActivities(lessonId: string) {
  return useQuery({
    queryKey: contentKeys.activities(lessonId),
    queryFn: ({ signal }) =>
      unwrap(
        api.GET('/api/v1/admin/content/lessons/{lessonId}/activities', { params: { path: { lessonId } }, signal }),
      ),
  })
}

/** Activity changes send the lesson's version, like word changes, and answer with all the activities. */
export function useAddQuiz(lessonId: string) {
  return useContentMutation((version: string) =>
    unwrap(
      api.POST('/api/v1/admin/content/lessons/{lessonId}/activities/quiz', {
        params: { path: { lessonId } },
        body: { version },
      }),
    ),
  )
}

export function useRemoveActivity(lessonId: string) {
  return useContentMutation(({ activityId, version }: { activityId: string; version: string }) =>
    unwrap(
      api.DELETE('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}', {
        params: { path: { lessonId, activityId }, query: { version } },
      }),
    ),
  )
}

export function useReorderActivities(lessonId: string) {
  return useContentMutation(({ activityIds, version }: { activityIds: string[]; version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/activities/order', {
        params: { path: { lessonId } },
        body: { activityIds, version },
      }),
    ),
  )
}

export interface MatchingInput {
  mode: MatchingMode
  /** 2 to 6 different words of the lesson, in the Admin's order. */
  wordIds: string[]
}

/** POST .../activities/matching (content.manage, drafts): a matching after the other activities (S3.3). */
export function useAddMatching(lessonId: string) {
  return useContentMutation((input: MatchingInput & { version: string }) =>
    unwrap(
      api.POST('/api/v1/admin/content/lessons/{lessonId}/activities/matching', {
        params: { path: { lessonId } },
        body: input,
      }),
    ),
  )
}

/** PUT .../activities/{activityId}/matching (content.manage, drafts): new mode and words. */
export function useUpdateMatching(lessonId: string) {
  return useContentMutation(({ activityId, ...input }: MatchingInput & { activityId: string; version: string }) =>
    unwrap(
      api.PUT('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/matching', {
        params: { path: { lessonId, activityId } },
        body: input,
      }),
    ),
  )
}

export interface QuestionInput {
  form: QuizForm
  correctWordId: string
  /** 1 to 3 other words of the lesson. */
  wrongWordIds: string[]
  /** Only for FillInTheBlank: the sentence with one `___`. */
  sentence: string | null
}

export function useAddQuestion(lessonId: string) {
  return useContentMutation(({ activityId, ...input }: QuestionInput & { activityId: string; version: string }) =>
    unwrap(
      api.POST('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions', {
        params: { path: { lessonId, activityId } },
        body: input,
      }),
    ),
  )
}

export function useUpdateQuestion(lessonId: string) {
  return useContentMutation(
    ({
      activityId,
      questionId,
      ...input
    }: QuestionInput & { activityId: string; questionId: string; version: string }) =>
      unwrap(
        api.PUT('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/{questionId}', {
          params: { path: { lessonId, activityId, questionId } },
          body: input,
        }),
      ),
  )
}

export function useRemoveQuestion(lessonId: string) {
  return useContentMutation(
    ({ activityId, questionId, version }: { activityId: string; questionId: string; version: string }) =>
      unwrap(
        api.DELETE('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/{questionId}', {
          params: { path: { lessonId, activityId, questionId }, query: { version } },
        }),
      ),
  )
}

export function useReorderQuestions(lessonId: string) {
  return useContentMutation(
    ({ activityId, questionIds, version }: { activityId: string; questionIds: string[]; version: string }) =>
      unwrap(
        api.PUT('/api/v1/admin/content/lessons/{lessonId}/activities/{activityId}/questions/order', {
          params: { path: { lessonId, activityId } },
          body: { questionIds, version },
        }),
      ),
  )
}

/**
 * Suggested wrong options for a question (owner, 2026-10-24: the system suggests, the Admin decides): the words that
 * follow the correct one in the word bank, wrapping around, so suggestions differ from question to question.
 */
export function suggestWrongWords(words: { id: string }[], correctWordId: string, count: number): string[] {
  const start = words.findIndex((word) => word.id === correctWordId)
  if (start < 0) return []
  const others = [...words.slice(start + 1), ...words.slice(0, start)]
  return others.slice(0, count).map((word) => word.id)
}

/** The IDs of `items` with the one at `index` moved by `offset` (−1 up, +1 down); null when it cannot move. */
export function moveInOrder(items: { id: string }[], index: number, offset: -1 | 1): string[] | null {
  const target = index + offset
  if (index < 0 || index >= items.length || target < 0 || target >= items.length) {
    return null
  }
  const ids = items.map((item) => item.id)
  ;[ids[index], ids[target]] = [ids[target] as string, ids[index] as string]
  return ids
}
