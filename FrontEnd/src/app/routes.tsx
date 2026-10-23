import type { RouteObject } from 'react-router'
import { adminRoutes } from '@/areas/admin/routes'
import { kidsRoutes } from '@/areas/kids/routes'
import { parentRoutes } from '@/areas/parent/routes'
import { publicRoutes } from '@/areas/public/routes'
import { PageLoading } from '@/shared/auth/guards'
import { NotFoundPage, RouteErrorPage } from '@/shared/components/ErrorPages'

/**
 * The whole app: each area brings its own routes (src/areas/<area>/routes.tsx) and loads its pages as a separate
 * chunk. Guards only shape navigation; the API checks the same permissions on every request.
 */
export const routes: RouteObject[] = [
  {
    errorElement: <RouteErrorPage />,
    // Shown while the first page's chunk downloads.
    hydrateFallbackElement: <PageLoading />,
    children: [...publicRoutes, adminRoutes, parentRoutes, kidsRoutes, { path: '*', element: <NotFoundPage /> }],
  },
]
