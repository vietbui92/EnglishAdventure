import type { RouteObject } from 'react-router'
import { RequireAuth } from '@/shared/auth/guards'
import { lazyPage } from '@/shared/routing'

// The pages come from index.ts, loaded the first time someone opens /kids.
const area = () => import('@/areas/kids')

/** /kids: any signed-in user until the child session exists (S2.2, F3.2); then only a child session. */
export const kidsRoutes: RouteObject = {
  element: <RequireAuth />,
  children: [
    {
      path: '/kids',
      lazy: lazyPage(area, (kids) => kids.KidsLayout),
      children: [{ index: true, lazy: lazyPage(area, (kids) => kids.KidsHomePage) }],
    },
  ],
}
