import {
  BookOpen,
  Gamepad2,
  Heart,
  LineChart,
  Lock,
  ShieldCheck,
  Trophy,
  UserPlus,
  Users,
  type LucideIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { PublicHeader } from '@/areas/public/PublicHeader'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/** The public home page: introduces the product to parents and leads to registration or sign-in. */
export function LandingPage() {
  const { t } = useTranslation()
  const session = useSession()

  const features: { icon: LucideIcon; tone: string; title: string; description: string }[] = [
    {
      icon: BookOpen,
      tone: 'bg-sky-100 text-sky-700',
      title: t('public.landing.features.levels.title'),
      description: t('public.landing.features.levels.description'),
    },
    {
      icon: Gamepad2,
      tone: 'bg-emerald-100 text-emerald-700',
      title: t('public.landing.features.games.title'),
      description: t('public.landing.features.games.description'),
    },
    {
      icon: Trophy,
      tone: 'bg-amber-100 text-amber-700',
      title: t('public.landing.features.rewards.title'),
      description: t('public.landing.features.rewards.description'),
    },
  ]

  const steps = [
    {
      icon: UserPlus,
      title: t('public.landing.steps.account.title'),
      description: t('public.landing.steps.account.description'),
    },
    {
      icon: Users,
      title: t('public.landing.steps.profile.title'),
      description: t('public.landing.steps.profile.description'),
    },
    {
      icon: LineChart,
      title: t('public.landing.steps.learn.title'),
      description: t('public.landing.steps.learn.description'),
    },
  ]

  const promises: { icon: LucideIcon; text: string }[] = [
    { icon: Lock, text: t('public.landing.parents.privacy') },
    { icon: ShieldCheck, text: t('public.landing.parents.control') },
    { icon: LineChart, text: t('public.landing.parents.progress') },
    { icon: Heart, text: t('public.landing.parents.content') },
  ]

  return (
    <div className="flex min-h-svh flex-col">
      <PublicHeader sectionLinks />

      <main className="flex-1">
        <section className="bg-gradient-to-b from-amber-50 to-background">
          <div className="mx-auto grid max-w-6xl items-center gap-10 px-4 py-16 md:grid-cols-2 md:py-24">
            <div className="flex flex-col gap-6">
              <p className="text-sm font-semibold tracking-wide text-amber-700 uppercase">
                {t('public.landing.hero.eyebrow')}
              </p>
              <h1 className="font-heading text-4xl font-bold tracking-tight md:text-5xl">
                {t('public.landing.hero.title')}
              </h1>
              <p className="text-lg text-muted-foreground">{t('public.landing.hero.subtitle')}</p>
              {!session && (
                <div className="flex flex-wrap gap-3">
                  <Button asChild size="lg">
                    <Link to="/register">{t('public.landing.hero.register')}</Link>
                  </Button>
                  <Button asChild size="lg" variant="outline">
                    <Link to="/login">{t('public.landing.hero.signIn')}</Link>
                  </Button>
                </div>
              )}
            </div>
            <HeroIllustration />
          </div>
        </section>

        <section id="features" aria-labelledby="features-title" className="mx-auto max-w-6xl scroll-mt-20 px-4 py-16">
          <h2 id="features-title" className="font-heading text-3xl font-bold">
            {t('public.landing.features.title')}
          </h2>
          <ul className="mt-8 grid gap-6 md:grid-cols-3">
            {features.map(({ icon: Icon, tone, title, description }) => (
              <li key={title} className="flex flex-col gap-3 rounded-2xl border bg-card p-6">
                <span className={cn('flex size-12 items-center justify-center rounded-xl', tone)}>
                  <Icon className="size-6" aria-hidden />
                </span>
                <h3 className="text-lg font-semibold">{title}</h3>
                <p className="text-muted-foreground">{description}</p>
              </li>
            ))}
          </ul>
        </section>

        <section id="how-it-works" aria-labelledby="steps-title" className="scroll-mt-20 bg-muted/50">
          <div className="mx-auto max-w-6xl px-4 py-16">
            <h2 id="steps-title" className="font-heading text-3xl font-bold">
              {t('public.landing.steps.title')}
            </h2>
            <ol className="mt-8 grid gap-6 md:grid-cols-3">
              {steps.map(({ icon: Icon, title, description }, index) => (
                <li key={title} className="flex gap-4">
                  <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary font-bold text-primary-foreground">
                    {index + 1}
                  </span>
                  <div className="flex flex-col gap-1">
                    <h3 className="flex items-center gap-2 font-semibold">
                      <Icon className="size-4 text-muted-foreground" aria-hidden />
                      {title}
                    </h3>
                    <p className="text-muted-foreground">{description}</p>
                  </div>
                </li>
              ))}
            </ol>
          </div>
        </section>

        <section id="parents" aria-labelledby="parents-title" className="mx-auto max-w-6xl scroll-mt-20 px-4 py-16">
          <h2 id="parents-title" className="font-heading text-3xl font-bold">
            {t('public.landing.parents.title')}
          </h2>
          <ul className="mt-8 grid gap-4 sm:grid-cols-2">
            {promises.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-start gap-3 rounded-xl border p-4">
                <Icon className="mt-0.5 size-5 shrink-0 text-emerald-600" aria-hidden />
                <span>{text}</span>
              </li>
            ))}
          </ul>
        </section>

        {!session && (
          <section aria-labelledby="cta-title" className="bg-amber-400 text-amber-950">
            <div className="mx-auto flex max-w-6xl flex-col items-center gap-6 px-4 py-14 text-center">
              <h2 id="cta-title" className="font-heading text-3xl font-bold">
                {t('public.landing.cta.title')}
              </h2>
              <Button asChild size="lg" className="bg-amber-950 text-amber-50 hover:bg-amber-900">
                <Link to="/register">{t('public.landing.cta.register')}</Link>
              </Button>
            </div>
          </section>
        )}
      </main>

      <footer className="border-t">
        <p className="mx-auto max-w-6xl px-4 py-6 text-sm text-muted-foreground">
          {t('public.landing.footer', { year: new Date().getFullYear() })}
        </p>
      </footer>
    </div>
  )
}

// Decorative: three playful cards standing for lessons, games and rewards.
function HeroIllustration() {
  return (
    <div aria-hidden className="relative mx-auto grid w-full max-w-sm grid-cols-2 gap-4">
      <div className="col-span-2 flex items-center gap-4 rounded-3xl bg-sky-200 p-6 shadow-sm">
        <BookOpen className="size-10 text-sky-800" />
        <div className="flex flex-col gap-2">
          <span className="h-3 w-32 rounded-full bg-sky-50/80" />
          <span className="h-3 w-20 rounded-full bg-sky-50/60" />
        </div>
      </div>
      <div className="flex aspect-square items-center justify-center rounded-3xl bg-emerald-200 shadow-sm">
        <Gamepad2 className="size-14 text-emerald-800" />
      </div>
      <div className="flex aspect-square rotate-3 items-center justify-center rounded-3xl bg-amber-300 shadow-sm">
        <Trophy className="size-14 text-amber-900" />
      </div>
    </div>
  )
}
