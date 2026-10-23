import { Navigate, type RouteObject } from 'react-router'
import { AdminLayout } from '@/app/layouts/AdminLayout'
import { ParentLayout } from '@/app/layouts/ParentLayout'
import { PlayLayout } from '@/app/layouts/PlayLayout'
import {
  AdminContentPage,
  AdminDashboardPage,
  AdminRolesPage,
  AdminUsersPage,
  ParentChildrenPage,
  ParentProgressPage,
  PlayHomePage,
} from '@/app/pages/AreaPages'
import { NotFoundPage, RouteErrorPage } from '@/app/pages/ErrorPages'
import { HomePage } from '@/app/pages/HomePage'
import { RequireAuth, RequirePermission } from '@/auth/guards'
import { LoginPage } from '@/auth/LoginPage'
import { Permissions } from '@/auth/permissions'

// Guards only shape navigation; the API checks the same permissions on every request.
export const routes: RouteObject[] = [
  {
    errorElement: <RouteErrorPage />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/login', element: <LoginPage /> },
      {
        element: <RequireAuth userTypes={['Admin']} />,
        children: [
          {
            path: '/admin',
            element: <AdminLayout />,
            children: [
              { index: true, element: <AdminDashboardPage /> },
              {
                path: 'users',
                element: (
                  <RequirePermission permission={Permissions.usersRead}>
                    <AdminUsersPage />
                  </RequirePermission>
                ),
              },
              {
                path: 'roles',
                element: (
                  <RequirePermission permission={Permissions.rolesManage}>
                    <AdminRolesPage />
                  </RequirePermission>
                ),
              },
              {
                path: 'content',
                element: (
                  <RequirePermission permission={Permissions.contentManage}>
                    <AdminContentPage />
                  </RequirePermission>
                ),
              },
            ],
          },
        ],
      },
      {
        element: <RequireAuth userTypes={['Parent']} />,
        children: [
          {
            path: '/parent',
            element: <ParentLayout />,
            children: [
              { index: true, element: <Navigate to="children" replace /> },
              {
                path: 'children',
                element: (
                  <RequirePermission permission={Permissions.familyChildrenManage}>
                    <ParentChildrenPage />
                  </RequirePermission>
                ),
              },
              {
                path: 'progress',
                element: (
                  <RequirePermission permission={Permissions.familyProgressRead}>
                    <ParentProgressPage />
                  </RequirePermission>
                ),
              },
            ],
          },
        ],
      },
      {
        // Any signed-in user until the child session exists (S2.2, F3.2); then only a child session.
        element: <RequireAuth />,
        children: [
          {
            path: '/play',
            element: <PlayLayout />,
            children: [{ index: true, element: <PlayHomePage /> }],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
