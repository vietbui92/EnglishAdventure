import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, unwrap } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'

export type UserSummary = components['schemas']['UserSummary']

export interface UserListParams {
  page: number
  pageSize: number
  search: string
}

export const userKeys = {
  all: ['admin', 'users'] as const,
  list: (params: UserListParams) => [...userKeys.all, 'list', params] as const,
  detail: (userId: string) => [...userKeys.all, 'detail', userId] as const,
}

/** GET /api/v1/admin/users (users.read). The previous page stays visible while the next one loads. */
export function useUserList(params: UserListParams) {
  return useQuery({
    queryKey: userKeys.list(params),
    queryFn: ({ signal }) =>
      unwrap(
        api.GET('/api/v1/admin/users', {
          params: { query: { page: params.page, pageSize: params.pageSize, search: params.search || undefined } },
          signal,
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

/** GET /api/v1/admin/users/{userId} (users.read). */
export function useUser(userId: string) {
  return useQuery({
    queryKey: userKeys.detail(userId),
    queryFn: ({ signal }) => unwrap(api.GET('/api/v1/admin/users/{userId}', { params: { path: { userId } }, signal })),
  })
}

export type CreatableUserType = 'Admin' | 'Parent'

export interface CreateUserInput {
  email: string
  password: string
  userType: CreatableUserType
}

/** POST /api/v1/admin/users (users.manage). Resolves to the new user; 409 when the email is taken. */
export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateUserInput) => unwrap(api.POST('/api/v1/admin/users', { body: input })),
    onSuccess: (created) => {
      queryClient.setQueryData(userKeys.detail(created.id), created)
      void queryClient.invalidateQueries({ queryKey: [...userKeys.all, 'list'] })
    },
  })
}

/** POST .../deactivate or .../activate (users.manage). Deactivating also ends the user's sessions. */
export function useSetUserActive(userId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (active: boolean) =>
      unwrap(
        active
          ? api.POST('/api/v1/admin/users/{userId}/activate', { params: { path: { userId } } })
          : api.POST('/api/v1/admin/users/{userId}/deactivate', { params: { path: { userId } } }),
      ),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: userKeys.all }),
  })
}

/** PUT .../roles (roles.manage). Replaces the user's roles and resolves to the updated user. */
export function useSetUserRoles(userId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (roles: string[]) =>
      unwrap(api.PUT('/api/v1/admin/users/{userId}/roles', { params: { path: { userId } }, body: { roles } })),
    onSuccess: (updated) => {
      queryClient.setQueryData(userKeys.detail(userId), updated)
      void queryClient.invalidateQueries({ queryKey: [...userKeys.all, 'list'] })
    },
  })
}
