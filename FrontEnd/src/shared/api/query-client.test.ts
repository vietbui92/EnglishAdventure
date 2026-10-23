import { describe, expect, it } from 'vitest'
import { clearCacheOnSignOut, createQueryClient } from '@/shared/api/query-client'
import { sessionStore } from '@/shared/auth/session'
import { signInAs } from '@/shared/test/session'

describe('clearCacheOnSignOut', () => {
  it('drops cached data when the session ends', () => {
    signInAs('SuperAdmin')
    const client = createQueryClient()
    const stop = clearCacheOnSignOut(client)
    client.setQueryData(['admin', 'users'], ['an@example.test'])

    signInAs('SuperAdmin')
    expect(client.getQueryData(['admin', 'users'])).toEqual(['an@example.test'])

    sessionStore.set({ status: 'signed-out', reason: 'expired' })
    expect(client.getQueryData(['admin', 'users'])).toBeUndefined()
    stop()
  })
})
