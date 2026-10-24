import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type BadgeDefinition = components['schemas']['BadgeDefinitionView']

export const rewardKeys = {
  all: ['admin', 'rewards'] as const,
  badges: () => [...rewardKeys.all, 'badges'] as const,
}

/** GET /api/v1/admin/rewards/badges (rewards.manage, S5.2): the four badges' texts and pictures. */
export function useBadgeDefinitions() {
  return useQuery({
    queryKey: rewardKeys.badges(),
    queryFn: ({ signal }) => unwrap(api.GET('/api/v1/admin/rewards/badges', { signal })),
  })
}

export interface BadgeInput {
  name: string
  description: string
  /** Blank to let the app draw the badge. */
  imageUrl: string
}

/** PUT /api/v1/admin/rewards/badges/{kind}: children who won the badge see the new texts too. */
export function useUpdateBadge() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ kind, ...input }: BadgeInput & { kind: string }) =>
      unwrap(
        api.PUT('/api/v1/admin/rewards/badges/{kind}', {
          params: { path: { kind } },
          body: { ...input, imageUrl: input.imageUrl || null },
        }),
      ),
    onSettled: () => queryClient.invalidateQueries({ queryKey: rewardKeys.all }),
  })
}
