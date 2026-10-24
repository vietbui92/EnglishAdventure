import { useQuery } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type LearnerMap = components['schemas']['LearnerMap']
export type MapLevel = components['schemas']['MapLevel']
export type PlayLesson = components['schemas']['PlayLesson']
export type LearnCard = components['schemas']['LearnCard']

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
