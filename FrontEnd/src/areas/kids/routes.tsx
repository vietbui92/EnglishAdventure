import type { RouteObject } from 'react-router'
import { RequireAuth } from '@/shared/auth/guards'
import { lazyPage } from '@/shared/routing'

// The pages come from index.ts, loaded the first time someone opens /kids.
const area = () => import('@/areas/kids')

/** /kids: only a child session, started by a Parent from "My children" (PD-010). */
export const kidsRoutes: RouteObject = {
  element: <RequireAuth childSession />,
  children: [
    {
      path: '/kids',
      lazy: lazyPage(area, (kids) => kids.KidsLayout),
      children: [
        { index: true, lazy: lazyPage(area, (kids) => kids.KidsHomePage) },
        { path: 'lessons/:lessonId', lazy: lazyPage(area, (kids) => kids.LessonPlayPage) },
      ],
    },
  ],
}
