import { QueryClientProvider, type QueryClient } from '@tanstack/react-query'
import { act, render, renderHook, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter, RouterProvider, createMemoryRouter, type RouteObject } from 'react-router'
import { createQueryClient } from '@/shared/api/query-client'

/** The app's query client without retries or caching between renders, so failures show at once. */
export function createTestQueryClient() {
  const client = createQueryClient()
  client.setDefaultOptions({
    queries: { retry: false, staleTime: 0, refetchOnWindowFocus: false },
    mutations: { retry: false },
  })
  return client
}

interface RenderOptions {
  /** The URL to start at. Defaults to `/`. */
  route?: string
  queryClient?: QueryClient
}

function mount(routes: RouteObject[], { route = '/', queryClient = createTestQueryClient() }: RenderOptions) {
  const router = createMemoryRouter(routes, { initialEntries: [route] })
  const user = userEvent.setup()
  const result = render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return { ...result, user, router, queryClient }
}

/**
 * Renders a route tree with the app's providers (query client, memory router; i18n is set up globally) and waits until
 * the lazy routes of the first page have loaded, so the page can be queried right away.
 */
export async function renderRoutes(routes: RouteObject[], options: RenderOptions = {}) {
  const rendered = mount(routes, options)
  await settle(rendered.router)
  return rendered
}

type MemoryRouter = ReturnType<typeof createMemoryRouter>

// Waits until no navigation is pending and React has rendered. A <Navigate> (a guard sending to /login, /parent to
// /parent/children) starts another navigation after the first render, so this repeats until the location holds.
async function settle(router: MemoryRouter) {
  for (let attempt = 0; attempt < 10; attempt += 1) {
    await waitFor(
      () => {
        const { initialized, navigation } = router.state
        if (!initialized || navigation.state !== 'idle') throw new Error('The page is still loading.')
        // The first lazy chunk of a test file is compiled on demand, which can take more than a second under load.
      },
      { timeout: 5000 },
    )
    const location = router.state.location.key
    await act(() => new Promise((resolve) => setTimeout(resolve, 0)))
    if (router.state.navigation.state === 'idle' && router.state.location.key === location) {
      return
    }
  }
  throw new Error('The router did not settle after 10 navigations.')
}

/** Renders one component with the app's providers, at any URL. */
export function renderWithProviders(ui: ReactElement, options: RenderOptions = {}) {
  return mount([{ path: '*', element: ui }], options)
}

/** Renders a hook with the app's providers. */
export function renderHookWithProviders<Result>(
  hook: () => Result,
  { route = '/', queryClient = createTestQueryClient() }: RenderOptions = {},
) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
    </QueryClientProvider>
  )
  return { ...renderHook(hook, { wrapper }), queryClient }
}
