import { Swords } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLearnerMap, type MapLevel } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { useSession } from '@/shared/auth/session'
import { ChildAvatar } from '@/shared/components/ChildAvatar'
import { Button } from '@/shared/components/ui/button'

/**
 * The child's own space: the adventure map (S3.4, F4.2). Every published lesson can be opened for now; which lessons
 * are locked, and the stars, come with S4.2.
 */
export function KidsHomePage() {
  const { t } = useTranslation()
  const child = useSession()?.user.childProfile
  const map = useLearnerMap()

  return (
    <div className="flex w-full max-w-3xl flex-col items-center gap-8">
      <div className="flex flex-col items-center gap-3 text-center">
        {child && <ChildAvatar avatarKey={child.avatarKey} size="lg" />}
        <h1 className="font-heading text-4xl font-bold">
          {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
        </h1>
        <p className="text-xl">{t('kids.title')}</p>
      </div>

      {map.isPending ? (
        <p role="status" className="text-xl text-muted-foreground">
          {t('kids.map.loading')}
        </p>
      ) : map.isError ? (
        <div role="alert" className="flex flex-col items-center gap-4 text-center">
          <p className="text-xl">{t('kids.map.loadError')}</p>
          <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void map.refetch()}>
            {t('kids.map.retry')}
          </Button>
        </div>
      ) : map.data.levels.length === 0 ? (
        <p className="text-center text-xl text-muted-foreground">{t('kids.map.empty')}</p>
      ) : (
        <ol className="flex w-full flex-col gap-8" aria-label={t('kids.map.label')}>
          {map.data.levels.map((level, index) => (
            <LevelIsland key={level.id} level={level} number={index + 1} />
          ))}
        </ol>
      )}
    </div>
  )
}

/** One level as an island: its lessons in order, then its boss. */
function LevelIsland({ level, number }: { level: MapLevel; number: number }) {
  const { t } = useTranslation()

  return (
    <li className="flex flex-col gap-4 rounded-3xl bg-card p-5 shadow-sm">
      <div>
        <p className="text-base font-semibold text-muted-foreground">{t('kids.map.level', { number })}</p>
        <h2 className="font-heading text-2xl font-bold">{level.title}</h2>
        {level.description && <p className="mt-1 text-muted-foreground">{level.description}</p>}
      </div>

      <ol className="flex flex-wrap gap-3" aria-label={t('kids.map.lessonsOf', { level: level.title })}>
        {level.lessons.map((lesson, index) => (
          <li key={lesson.id}>
            <Link
              to={`/kids/lessons/${lesson.id}`}
              className="flex min-h-16 items-center gap-3 rounded-2xl bg-secondary px-5 text-lg font-semibold text-secondary-foreground transition-transform hover:scale-105 focus-visible:ring-4 focus-visible:ring-ring focus-visible:outline-none"
            >
              <span
                aria-hidden
                className="flex size-10 items-center justify-center rounded-full bg-card text-xl font-bold text-foreground"
              >
                {index + 1}
              </span>
              {lesson.title}
            </Link>
          </li>
        ))}
      </ol>

      {level.boss && (
        <Link
          to={`/kids/lessons/${level.boss.id}`}
          className="flex min-h-20 items-center gap-4 self-start rounded-2xl bg-primary px-5 py-2 text-lg font-bold text-primary-foreground transition-transform hover:scale-105 focus-visible:ring-4 focus-visible:ring-ring focus-visible:outline-none"
        >
          <KidsPicture url={level.boss.imageUrl} alt="" className="size-16" />
          <span className="flex flex-col items-start">
            <span className="flex items-center gap-1 text-base font-semibold">
              <Swords aria-hidden className="size-4" />
              {t('kids.map.boss')}
            </span>
            {level.boss.name}
          </span>
        </Link>
      )}
    </li>
  )
}
