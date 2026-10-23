import { Navigate, type RouteObject } from 'react-router'
import { RequireAuth } from '@/shared/auth/guards'
import { Permissions } from '@/shared/auth/permissions'
import { lazyPage, withPermission } from '@/shared/routing'

// The pages come from index.ts, loaded the first time someone opens /parent.
const area = () => import('@/areas/parent')

/** /parent: Parent users. */
export const parentRoutes: RouteObject = {
  element: <RequireAuth userTypes={['Parent']} />,
  children: [
    {
      path: '/parent',
      lazy: lazyPage(area, (parent) => parent.ParentLayout),
      children: [
        { index: true, element: <Navigate to="children" replace /> },
        withPermission(Permissions.familyChildrenManage, [
          { path: 'children', lazy: lazyPage(area, (parent) => parent.ChildrenPage) },
        ]),
        withPermission(Permissions.familyProgressRead, [
          { path: 'progress', lazy: lazyPage(area, (parent) => parent.ParentProgressPage) },
        ]),
      ],
    },
  ],
}
