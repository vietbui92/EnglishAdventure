import type { ComponentType } from 'react'
import { Outlet, type RouteObject } from 'react-router'
import { RequirePermission } from '@/shared/auth/guards'
import type { Permission } from '@/shared/auth/permissions'

/**
 * A route `lazy` function that renders one component of a lazily loaded module, usually an area's index.ts:
 * `lazy: lazyPage(() => import('@/areas/admin'), (area) => area.AdminLayout)`. The module becomes its own chunk.
 */
export function lazyPage<Module>(load: () => Promise<Module>, pick: (module: Module) => ComponentType) {
  return async () => ({ Component: pick(await load()) })
}

/** Child routes that need a permission; without it they show the 403 page inside the area layout. */
export function withPermission(permission: Permission, children: RouteObject[]): RouteObject {
  return {
    element: (
      <RequirePermission permission={permission}>
        <Outlet />
      </RequirePermission>
    ),
    children,
  }
}
