import { useMutation, useQuery } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type LearnerMap = components['schemas']['LearnerMap']
export type MapLevel = components['schemas']['MapLevel']
export type PlayLesson = components['schemas']['PlayLesson']
export type LearnCard = components['schemas']['LearnCard']
export type PlayActivity = components['schemas']['PlayActivity']
export type PlayQuestion = components['schemas']['PlayQuestion']
export type PlayChoice = components['schemas']['PlayChoice']
export type AttemptView = components['schemas']['AttemptView']
export type AnswerResult = components['schemas']['AnswerResult']

export const playKeys = {
  all: ['kids', 'play'] as const,
  map: () => [...playKeys.all, 'map'] as const,
  lesson: (lessonId: string) => [...playKeys.all, 'lesson', lessonId] as const,
}

/** GET /api/v1/play/map (child session): published levels with their lessons and boss (S3.4). */
export function useLearnerMap() {
  return useQuery({
    queryKey: playKeys.map(),
    queryFn: ({ signal }) => unwrap(api.GET('/api/v1/play/map', { signal })),
  })
}

/**
 * GET /api/v1/play/lessons/{lessonId} (child session): learn cards, then activities, without answers. The server
 * shuffles the options on every read, so the lesson is read once per visit and not again while the child plays.
 */
export function usePlayLesson(lessonId: string) {
  return useQuery({
    queryKey: playKeys.lesson(lessonId),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/play/lessons/{lessonId}', { params: { path: { lessonId } }, signal })),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  })
}

/**
 * POST /api/v1/play/lessons/{lessonId}/attempts (S4.1): starts playing, or resumes the unfinished attempt. Called when
 * the games begin, not on opening the lesson, so looking at the cards does not count as starting it.
 */
export function useStartAttempt(lessonId: string) {
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/v1/play/lessons/{lessonId}/attempts', { params: { path: { lessonId } } })),
  })
}

/** POST .../attempts/{attemptId}/questions/{questionId}: the server grades the answer; the first one counts. */
export function useAnswerQuestion(attemptId: string) {
  return useMutation({
    mutationFn: ({ questionId, optionId }: { questionId: string; optionId: string }) =>
      unwrap(
        api.POST('/api/v1/play/attempts/{attemptId}/questions/{questionId}', {
          params: { path: { attemptId, questionId } },
          body: { optionId },
        }),
      ),
  })
}

/** POST .../attempts/{attemptId}/matches: one match of a matching; a wrong one can be tried again. */
export function useMatchPair(attemptId: string) {
  return useMutation({
    mutationFn: ({ leftId, rightId }: { leftId: string; rightId: string }) =>
      unwrap(
        api.POST('/api/v1/play/attempts/{attemptId}/matches', {
          params: { path: { attemptId } },
          body: { leftId, rightId },
        }),
      ),
  })
}

/** One screen of the games: a quiz question, or a whole matching. */
export type GameStep =
  | { kind: 'question'; activityId: string; question: PlayQuestion; number: number; count: number }
  | { kind: 'matching'; activity: PlayActivity }

/** The games of a lesson as screens, in order: each quiz question, then each matching as one screen. */
export function gameSteps(activities: PlayActivity[]): GameStep[] {
  return activities.flatMap((activity): GameStep[] =>
    activity.matching
      ? [{ kind: 'matching', activity }]
      : activity.questions.map((question, index) => ({
          kind: 'question',
          activityId: activity.id,
          question,
          number: index + 1,
          count: activity.questions.length,
        })),
  )
}

/** Whether a screen is done in the attempt: the question answered, or every pair of the matching matched. */
export function isStepDone(step: GameStep, attempt: AttemptView) {
  if (step.kind === 'question') {
    return attempt.questions.some((answered) => answered.questionId === step.question.id)
  }
  const matched = new Set(attempt.matchedPairs.map((pair) => pair.leftId))
  return (step.activity.matching?.left ?? []).every((left) => matched.has(left.id))
}

/** The first screen not done yet, from `from` on; the number of steps when everything is done. */
export function nextStep(steps: GameStep[], attempt: AttemptView, from = 0) {
  const index = steps.findIndex((step, position) => position >= from && !isStepDone(step, attempt))
  return index < 0 ? steps.length : index
}
