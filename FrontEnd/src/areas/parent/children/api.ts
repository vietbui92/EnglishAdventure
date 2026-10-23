import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type ChildProfile = components['schemas']['ChildProfileSummary']

export interface ChildProfileInput {
  nickname: string
  avatarKey: string
}

const childKeys = {
  all: ['family', 'children'] as const,
  list: () => [...childKeys.all, 'list'] as const,
  options: () => [...childKeys.all, 'options'] as const,
}

/** All of the signed-in Parent's profiles, active and archived (the page shows them in two groups). */
export function useChildProfiles() {
  return useQuery({
    queryKey: childKeys.list(),
    queryFn: ({ signal }) =>
      unwrap(api.GET('/api/v1/family/children', { params: { query: { includeArchived: true } }, signal })),
  })
}

/** The avatars and the limit of active profiles, as the API enforces them. */
export function useChildProfileOptions() {
  return useQuery({
    queryKey: childKeys.options(),
    queryFn: ({ signal }) => unwrap(api.GET('/api/v1/family/children/options', { signal })),
    staleTime: Infinity,
  })
}

function useChildMutation<Variables>(send: (variables: Variables) => Promise<unknown>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: send,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: childKeys.list() }),
  })
}

/** POST: 409 when the nickname is taken in the family, 422 when the family is at the limit. */
export function useCreateChildProfile() {
  return useChildMutation((input: ChildProfileInput) => unwrap(api.POST('/api/v1/family/children', { body: input })))
}

/** PUT: 409 when the nickname is taken in the family. */
export function useUpdateChildProfile(profileId: string) {
  return useChildMutation((input: ChildProfileInput) =>
    unwrap(api.PUT('/api/v1/family/children/{profileId}', { params: { path: { profileId } }, body: input })),
  )
}

export function useArchiveChildProfile() {
  return useChildMutation((profileId: string) =>
    unwrap(api.POST('/api/v1/family/children/{profileId}/archive', { params: { path: { profileId } } })),
  )
}

/** 422 when the family is at the limit, 409 when an active profile uses the same nickname. */
export function useRestoreChildProfile() {
  return useChildMutation((profileId: string) =>
    unwrap(api.POST('/api/v1/family/children/{profileId}/restore', { params: { path: { profileId } } })),
  )
}
