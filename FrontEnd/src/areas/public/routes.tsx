import type { RouteObject } from 'react-router'
import { LandingPage } from '@/areas/public/LandingPage'
import { lazyPage } from '@/shared/routing'

// Sign-in, registration and the legal pages come from index.ts, loaded the first time one of them opens.
const area = () => import('@/areas/public')

/**
 * Pages anyone can open. The landing page is in the main bundle because most visitors arrive there.
 * /confirm-email is the page the registration email links to (backend WebAppLinks).
 */
export const publicRoutes: RouteObject[] = [
  { path: '/', element: <LandingPage /> },
  { path: '/login', lazy: lazyPage(area, (pages) => pages.LoginPage) },
  { path: '/register', lazy: lazyPage(area, (pages) => pages.RegisterPage) },
  { path: '/register/check-email', lazy: lazyPage(area, (pages) => pages.CheckEmailPage) },
  { path: '/confirm-email', lazy: lazyPage(area, (pages) => pages.ConfirmEmailPage) },
  { path: '/terms', lazy: lazyPage(area, (pages) => pages.TermsPage) },
  { path: '/privacy', lazy: lazyPage(area, (pages) => pages.PrivacyPage) },
]
