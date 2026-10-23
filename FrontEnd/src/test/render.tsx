import { QueryClientProvider, type QueryClient } from '@tanstack/react-query'
import { render, renderHook } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter, RouterProvider, createMemoryRouter, type RouteObject } from 'react-router'
import { createQueryClient } from '@/app/query-client'

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

/** Renders a route tree with the app's providers (query client, memory router; i18n is set up globally). */
export function renderRoutes(
  routes: RouteObject[],
  { route = '/', queryClient = createTestQueryClient() }: RenderOptions = {},
) {
  const router = createMemoryRouter(routes, { initialEntries: [route] })
  const user = userEvent.setup()
  const result = render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return { ...result, user, router, queryClient }
}

/** Renders one component with the app's providers, at any URL. */
export function renderWithProviders(ui: ReactElement, options?: RenderOptions) {
  return renderRoutes([{ path: '*', element: ui }], options)
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
