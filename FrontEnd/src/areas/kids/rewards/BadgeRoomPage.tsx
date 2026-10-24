import { motion } from 'motion/react'
import { Map as MapIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLearnerMap, useMyRewards, type BadgeView } from '@/areas/kids/play/api'
import { Button } from '@/shared/components/ui/button'
import { BadgeMedal } from '@/shared/components/BadgeMedal'
import { cn } from '@/shared/lib/utils'

/**
 * The badge room (S5.3, owner 2026-10-24): the child's points and every badge they can win, the won ones bright with
 * their date, the others grey with what wins them. The two general badges first, then each island's two.
 */
export function BadgeRoomPage() {
  const { t } = useTranslation()
  const rewards = useMyRewards()
  const map = useLearnerMap()

  const general = rewards.data?.badges.filter((badge) => badge.levelId === null) ?? []
  const levels = (map.data?.levels ?? []).map((level) => ({
    id: level.id,
    name: level.description ?? level.title,
    badges: rewards.data?.badges.filter((badge) => badge.levelId === level.id) ?? [],
  }))
  const won = rewards.data?.badges.filter((badge) => badge.earned).length ?? 0

  return (
    <div className="kids-zoom mx-auto flex min-h-svh w-full max-w-4xl flex-col gap-6 px-4 pt-20 pb-10 sm:pt-24">
      <div className="absolute top-0 left-0 z-30 p-3 sm:p-4">
        <Button
          asChild
          variant="ghost"
          className="min-h-12 rounded-full bg-white/85 px-4 text-base shadow-md backdrop-blur hover:bg-white"
        >
          <Link to="/kids">
            <MapIcon aria-hidden className="size-5" />
            <span className="sr-only sm:not-sr-only">{t('kids.play.backToMap')}</span>
          </Link>
        </Button>
      </div>

      <header className="flex flex-col items-center gap-2 text-center">
        <h1 className="font-heading text-4xl font-bold">{t('kids.rewards.room')}</h1>
        {rewards.data && (
          <p className="text-xl">
            <span className="rounded-full bg-yellow-100 px-4 py-1 font-bold text-amber-700">
              {t('kids.rewards.total', { points: Number(rewards.data.points) })}
            </span>{' '}
            {t('kids.rewards.won', { won, count: rewards.data.badges.length })}
          </p>
        )}
      </header>

      {rewards.isPending ? (
        <p role="status" className="text-center text-xl text-muted-foreground">
          {t('kids.rewards.loading')}
        </p>
      ) : rewards.isError ? (
        <div role="alert" className="flex flex-col items-center gap-4 text-center">
          <p className="text-xl">{t('kids.rewards.loadError')}</p>
          <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void rewards.refetch()}>
            {t('kids.map.retry')}
          </Button>
        </div>
      ) : (
        <>
          <Shelf title={t('kids.rewards.general')} badges={general} />
          {levels.map((level) =>
            level.badges.length > 0 ? <Shelf key={level.id} title={level.name} badges={level.badges} /> : null,
          )}
        </>
      )}
    </div>
  )
}

function Shelf({ title, badges }: { title: string; badges: BadgeView[] }) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-heading text-2xl font-bold">{title}</h2>
      <ul className="grid gap-4 sm:grid-cols-2">
        {badges.map((badge, index) => (
          <BadgeCard key={`${badge.kind}-${badge.levelId ?? ''}`} badge={badge} index={index} />
        ))}
      </ul>
    </section>
  )
}

function BadgeCard({ badge, index }: { badge: BadgeView; index: number }) {
  const { t, i18n } = useTranslation()
  const date = badge.awardedAtUtc
    ? new Intl.DateTimeFormat(i18n.language, { dateStyle: 'medium' }).format(new Date(badge.awardedAtUtc))
    : null

  return (
    <motion.li
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.08 }}
      className={cn(
        'flex items-center gap-4 rounded-3xl p-4 shadow-sm',
        badge.earned ? 'bg-card' : 'bg-muted/60 text-muted-foreground',
      )}
    >
      <BadgeMedal
        kind={badge.kind}
        imageUrl={badge.imageUrl}
        earned={badge.earned}
        size="lg"
        className={cn(badge.earned && 'kids-bob')}
      />
      <span className="flex flex-col gap-1">
        <span className={cn('font-heading text-xl font-bold', badge.earned && 'text-foreground')}>{badge.name}</span>
        <span>{badge.description}</span>
        <span className="text-sm font-semibold">
          {badge.earned && date ? t('kids.rewards.earnedOn', { date }) : t('kids.rewards.notYet')}
        </span>
      </span>
    </motion.li>
  )
}
