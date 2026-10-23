import { ChevronLeft, ChevronRight, Plus, Search, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { hasPermission, Permissions } from '@/shared/auth/permissions'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { useUserList, type UserSummary } from '@/areas/admin/users/api'
import { UserRoles, UserStatusBadges, useUserLabels } from '@/areas/admin/users/labels'
import { useDateTimeFormat } from '@/shared/lib/format'

const pageSize = 20

// Page and search live in the URL (?q=...&page=2), so they survive a reload and work with the back button.
function readPage(value: string | null) {
  const page = Number(value)
  return Number.isInteger(page) && page >= 1 ? page : 1
}

export function UsersListPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('q') ?? ''
  const page = readPage(searchParams.get('page'))
  const users = useUserList({ page, pageSize, search })
  const session = useSession()
  const canCreate = session ? hasPermission(session.user, Permissions.usersManage) : false

  const goTo = (next: { search?: string; page?: number }) => {
    const params = new URLSearchParams()
    const nextSearch = next.search ?? search
    if (nextSearch) params.set('q', nextSearch)
    if (next.page && next.page > 1) params.set('page', String(next.page))
    setSearchParams(params)
  }

  const totalCount = Number(users.data?.totalCount ?? 0)
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-heading text-2xl font-semibold">{t('admin.users.title')}</h1>
          {canCreate && (
            <Button asChild size="sm">
              <Link to="new">
                <Plus aria-hidden />
                {t('admin.users.addUser')}
              </Link>
            </Button>
          )}
        </div>
        {/* Re-created when the URL search changes (e.g. back button), so the field shows the current search. */}
        <SearchForm key={search} initial={search} onSearch={(value) => goTo({ search: value, page: 1 })} />
      </div>

      {users.isPending ? (
        <p role="status" className="text-muted-foreground">
          {t('admin.users.loading')}
        </p>
      ) : users.isError ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          <p>{t('admin.users.loadError')}</p>
          <Button variant="outline" size="sm" onClick={() => void users.refetch()}>
            {t('admin.users.retry')}
          </Button>
        </div>
      ) : users.data.items.length === 0 ? (
        <p className="text-muted-foreground">
          {search ? t('admin.users.noResults', { search }) : t('admin.users.empty')}
        </p>
      ) : (
        <>
          <UsersTable users={users.data.items} />
          <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
            <span>{t('admin.users.total', { count: totalCount })}</span>
            <nav aria-label={t('admin.users.pagination.label')} className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1 || users.isPlaceholderData}
                onClick={() => goTo({ page: page - 1 })}
              >
                <ChevronLeft aria-hidden />
                {t('admin.users.pagination.previous')}
              </Button>
              <span aria-live="polite">{t('admin.users.pagination.status', { page, pages })}</span>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= pages || users.isPlaceholderData}
                onClick={() => goTo({ page: page + 1 })}
              >
                {t('admin.users.pagination.next')}
                <ChevronRight aria-hidden />
              </Button>
            </nav>
          </div>
        </>
      )}
    </section>
  )
}

function SearchForm({ initial, onSearch }: { initial: string; onSearch: (value: string) => void }) {
  const { t } = useTranslation()
  const [value, setValue] = useState(initial)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSearch(value.trim())
  }

  return (
    <form role="search" onSubmit={submit} className="flex w-full max-w-md items-center gap-2 sm:w-auto">
      <Input
        type="search"
        aria-label={t('admin.users.searchLabel')}
        placeholder={t('admin.users.searchPlaceholder')}
        maxLength={256}
        value={value}
        onChange={(event) => setValue(event.target.value)}
        className="min-w-0 flex-1 sm:w-64"
      />
      <Button type="submit" variant="secondary">
        <Search aria-hidden />
        {t('admin.users.search')}
      </Button>
      {initial && (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={t('admin.users.clearSearch')}
          onClick={() => onSearch('')}
        >
          <X aria-hidden />
        </Button>
      )}
    </form>
  )
}

function UsersTable({ users }: { users: UserSummary[] }) {
  const { t } = useTranslation()
  const labels = useUserLabels()
  const formatDateTime = useDateTimeFormat()

  return (
    <div className="overflow-x-auto rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{t('admin.users.columns.email')}</TableHead>
            <TableHead>{t('admin.users.columns.userType')}</TableHead>
            <TableHead>{t('admin.users.columns.roles')}</TableHead>
            <TableHead>{t('admin.users.columns.status')}</TableHead>
            <TableHead>{t('admin.users.columns.lastLogin')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell className="font-medium">
                <Link to={user.id} className="underline-offset-4 hover:underline">
                  {user.email}
                </Link>
              </TableCell>
              <TableCell>{labels.userType(user.userType)}</TableCell>
              <TableCell>
                <UserRoles roles={user.roles} />
              </TableCell>
              <TableCell>
                <UserStatusBadges user={user} />
              </TableCell>
              <TableCell className="whitespace-nowrap text-muted-foreground">
                {user.lastLoginAtUtc ? formatDateTime(user.lastLoginAtUtc) : t('admin.users.never')}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
