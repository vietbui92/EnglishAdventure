import type { RouteObject } from 'react-router'
import { RequireAuth } from '@/shared/auth/guards'
import { Permissions } from '@/shared/auth/permissions'
import { lazyPage, withPermission } from '@/shared/routing'

// The pages come from index.ts, loaded the first time someone opens /admin.
const area = () => import('@/areas/admin')

/** /admin: SuperAdmin and ContentAdmin. Each page also checks its permission. */
export const adminRoutes: RouteObject = {
  element: <RequireAuth userTypes={['Admin']} />,
  children: [
    {
      path: '/admin',
      lazy: lazyPage(area, (admin) => admin.AdminLayout),
      children: [
        { index: true, lazy: lazyPage(area, (admin) => admin.AdminDashboardPage) },
        withPermission(Permissions.usersRead, [
          {
            path: 'users',
            children: [
              { index: true, lazy: lazyPage(area, (admin) => admin.UsersListPage) },
              withPermission(Permissions.usersManage, [
                { path: 'new', lazy: lazyPage(area, (admin) => admin.CreateUserPage) },
              ]),
              { path: ':userId', lazy: lazyPage(area, (admin) => admin.UserDetailPage) },
            ],
          },
        ]),
        withPermission(Permissions.rolesManage, [
          { path: 'roles', lazy: lazyPage(area, (admin) => admin.AdminRolesPage) },
        ]),
        withPermission(Permissions.rewardsManage, [
          { path: 'rewards', lazy: lazyPage(area, (admin) => admin.BadgesPage) },
        ]),
        withPermission(Permissions.contentReadDrafts, [
          {
            path: 'content',
            children: [
              { index: true, lazy: lazyPage(area, (admin) => admin.LevelsPage) },
              { path: 'levels/:levelId', lazy: lazyPage(area, (admin) => admin.LevelDetailPage) },
              { path: 'lessons/:lessonId', lazy: lazyPage(area, (admin) => admin.LessonPage) },
            ],
          },
        ]),
      ],
    },
  ],
}
