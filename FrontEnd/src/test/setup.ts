import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest'
import { sessionStore } from '@/auth/session'
import i18n from '@/i18n'
import { server } from '@/test/server'

// Tests start signed out (the app starts 'restoring'); sign in with signInAs() from '@/test/session'.
beforeEach(() => sessionStore.set({ status: 'signed-out' }))

// A request without a matching handler fails the test instead of reaching the network.
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))

afterEach(async () => {
  cleanup()
  server.resetHandlers()
  localStorage.clear()
  sessionStore.set({ status: 'signed-out' })
  await i18n.changeLanguage('vi')
})

afterAll(() => server.close())
