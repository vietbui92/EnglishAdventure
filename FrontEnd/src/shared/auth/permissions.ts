import type { CurrentUser } from '@/shared/auth/session'

/**
 * Permission codes from the backend catalog (BackEnd Application/Authorization/Permissions.cs). The API returns the
 * user's permissions on /me and enforces them on every endpoint; the UI only uses them to show or hide things.
 */
export const Permissions = {
  usersRead: 'users.read',
  usersManage: 'users.manage',
  rolesManage: 'roles.manage',
  contentReadDrafts: 'content.read-drafts',
  contentManage: 'content.manage',
  contentPublish: 'content.publish',
  rewardsManage: 'rewards.manage',
  familyChildrenManage: 'family.children.manage',
  familyProgressRead: 'family.progress.read',
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions]

export type UserType = 'Admin' | 'Parent'

export function hasPermission(user: CurrentUser, permission: Permission) {
  return user.permissions.includes(permission)
}
