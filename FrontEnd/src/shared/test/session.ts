import { Permissions, type Permission } from '@/shared/auth/permissions'
import { sessionStore, type CurrentUser, type Session } from '@/shared/auth/session'

const all = Object.values(Permissions)

// The default permissions of the backend's system roles (Application/Authorization/SystemRoles.cs).
const rolePermissions: Record<'SuperAdmin' | 'ContentAdmin' | 'Parent', Permission[]> = {
  SuperAdmin: all.filter((permission) => !permission.startsWith('family.')),
  ContentAdmin: all.filter((permission) => permission.startsWith('content.')),
  Parent: all.filter((permission) => permission.startsWith('family.')),
}

export type TestRole = keyof typeof rolePermissions

export function testUser(role: TestRole, overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    userId: `00000000-0000-0000-0000-00000000000${Object.keys(rolePermissions).indexOf(role) + 1}`,
    email: `${role.toLowerCase()}@example.test`,
    userType: role === 'Parent' ? 'Parent' : 'Admin',
    roles: [role],
    permissions: rolePermissions[role],
    ...overrides,
  }
}

/** Puts the app in a signed-in state without calling the API. The token stays valid for an hour. */
export function signInAs(role: TestRole, overrides: Partial<CurrentUser> = {}): Session {
  const session = {
    accessToken: `test-token-${role}`,
    accessTokenExpiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
    user: testUser(role, overrides),
  }
  sessionStore.set({ status: 'signed-in', session })
  return session
}
