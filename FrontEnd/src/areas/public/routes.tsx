import type { RouteObject } from 'react-router'
import { LandingPage } from '@/areas/public/LandingPage'
import { lazyPage } from '@/shared/routing'

/**
 * Pages anyone can open. The landing page is in the main bundle because most visitors arrive there; sign-in and
 * registration bring the form libraries and load on demand.
 */
export const publicRoutes: RouteObject[] = [
  { path: '/', element: <LandingPage /> },
  {
    path: '/login',
    lazy: lazyPage(
      () => import('@/areas/public/LoginPage'),
      (module) => module.LoginPage,
    ),
  },
  {
    path: '/register',
    lazy: lazyPage(
      () => import('@/areas/public/RegisterPage'),
      (module) => module.RegisterPage,
    ),
  },
]
