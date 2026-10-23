import type { LucideIcon } from 'lucide-react'
import { NavLink } from 'react-router'
import { hasPermission, type Permission } from '@/auth/permissions'
import { useSession } from '@/auth/session'
import { cn } from '@/lib/utils'

export interface AreaNavItem {
  to: string
  label: string
  icon: LucideIcon
  /** Match the path exactly, for an area's index route. */
  end?: boolean
  /** Shown only to users with this permission (the route checks it too). */
  permission?: Permission
}

export function AreaNav({ items, label, className }: { items: AreaNavItem[]; label: string; className?: string }) {
  const session = useSession()
  const visible = items.filter((item) => !item.permission || (session && hasPermission(session.user, item.permission)))

  return (
    <nav aria-label={label} className={cn('flex gap-1', className)}>
      {visible.map(({ to, label: itemLabel, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium whitespace-nowrap transition-colors',
              isActive ? 'bg-secondary text-secondary-foreground' : 'text-muted-foreground hover:bg-muted',
            )
          }
        >
          <Icon className="size-4" aria-hidden />
          {itemLabel}
        </NavLink>
      ))}
    </nav>
  )
}
