import { Navigate, createBrowserRouter } from 'react-router'
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

// Route guards by user type and permission are added in F1.3.
export const router = createBrowserRouter([
  {
    errorElement: <RouteErrorPage />,
    children: [
      { path: '/', element: <HomePage /> },
      {
        path: '/admin',
        element: <AdminLayout />,
        children: [
          { index: true, element: <AdminDashboardPage /> },
          { path: 'users', element: <AdminUsersPage /> },
          { path: 'roles', element: <AdminRolesPage /> },
          { path: 'content', element: <AdminContentPage /> },
        ],
      },
      {
        path: '/parent',
        element: <ParentLayout />,
        children: [
          { index: true, element: <Navigate to="children" replace /> },
          { path: 'children', element: <ParentChildrenPage /> },
          { path: 'progress', element: <ParentProgressPage /> },
        ],
      },
      {
        path: '/play',
        element: <PlayLayout />,
        children: [{ index: true, element: <PlayHomePage /> }],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
