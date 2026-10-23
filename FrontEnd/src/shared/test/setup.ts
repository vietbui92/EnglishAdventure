import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest'
import { sessionStore } from '@/shared/auth/session'
import i18n from '@/shared/i18n'
import { server } from '@/shared/test/server'

// jsdom has no layout engine; Radix components (radio group, checkbox) only need ResizeObserver to exist.
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}

// Tests start signed out (the app starts 'restoring'); sign in with signInAs() from '@/shared/test/session'.
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
