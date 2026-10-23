import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useSession } from '@/shared/auth/session'
import { homePathFor } from '@/shared/auth/sign-in'
import { LanguageSwitcher } from '@/shared/components/LanguageSwitcher'
import { Button } from '@/shared/components/ui/button'

/** The public header: logo, section links on the landing page, language, and sign-in / register or "my area". */
export function PublicHeader({ sectionLinks = false }: { sectionLinks?: boolean }) {
  const { t } = useTranslation()
  const session = useSession()

  return (
    <header className="sticky top-0 z-10 border-b bg-background/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
        <Link to="/" className="flex items-center gap-2 font-heading text-lg font-bold">
          <span className="flex size-8 items-center justify-center rounded-lg bg-amber-400 text-amber-950">
            <Sparkles className="size-5" aria-hidden />
          </span>
          {t('app.name')}
        </Link>

        {sectionLinks && (
          <nav aria-label={t('common.mainNavigation')} className="hidden gap-5 text-sm text-muted-foreground md:flex">
            <a href="#features" className="hover:text-foreground">
              {t('public.nav.features')}
            </a>
            <a href="#how-it-works" className="hover:text-foreground">
              {t('public.nav.howItWorks')}
            </a>
            <a href="#parents" className="hover:text-foreground">
              {t('public.nav.parents')}
            </a>
          </nav>
        )}

        <div className="ml-auto flex items-center gap-2">
          <LanguageSwitcher />
          {session ? (
            <Button asChild>
              <Link to={homePathFor(session.user)}>{t('public.nav.myArea')}</Link>
            </Button>
          ) : (
            <>
              <Button asChild variant="ghost">
                <Link to="/login">{t('public.nav.signIn')}</Link>
              </Button>
              <Button asChild>
                <Link to="/register">{t('public.nav.register')}</Link>
              </Button>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
