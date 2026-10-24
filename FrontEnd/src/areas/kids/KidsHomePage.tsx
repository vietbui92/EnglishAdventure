import { useEffect, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { IslandScene } from '@/areas/kids/adventure/IslandScene'
import { readPlace, savePlace, type AdventurePlace } from '@/areas/kids/adventure/place'
import { SeaScene } from '@/areas/kids/adventure/SeaScene'
import { startAmbience } from '@/areas/kids/adventure/sound'
import { useLearnerMap } from '@/areas/kids/play/api'
import { useSession } from '@/shared/auth/session'
import { ChildAvatar } from '@/shared/components/ChildAvatar'
import { Button } from '@/shared/components/ui/button'

/**
 * The child's own space: the adventure map (S3.4, F4.2; owner 2026-10-24). The child starts on the mainland and sails
 * to the levels' islands, then walks to each lesson on an island. The map opens where the child left it. Every
 * published lesson can be opened for now; locks and stars come with S4.2.
 */
export function KidsHomePage() {
  const { t } = useTranslation()
  const child = useSession()?.user.childProfile
  const map = useLearnerMap()
  const [place, setPlace] = useState<AdventurePlace>(() => readPlace(child?.id))
  const [atSea, setAtSea] = useState(place.atSea === true)

  // The sea and the music while the map is open.
  useEffect(() => startAmbience(), [])

  const moveTo = (next: AdventurePlace) => {
    setPlace(next)
    savePlace(child?.id, next)
  }

  const greeting = (
    <span className="flex w-max items-center gap-2 rounded-full bg-white/90 py-1 pr-5 pl-1 shadow-md">
      {child && (
        <ChildAvatar avatarKey={child.avatarKey} size="sm" className="size-10 text-2xl sm:size-12 sm:text-3xl" />
      )}
      <span className="font-heading text-base font-bold sm:text-xl lg:text-2xl">
        {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
      </span>
      {map.data && (
        <span className="ml-1 rounded-full bg-yellow-100 px-3 py-0.5 text-sm font-bold text-amber-700 sm:text-base">
          {t('kids.rewards.total', { points: Number(map.data.points) })}
        </span>
      )}
    </span>
  )

  if (map.isPending) {
    return (
      <Centered>
        <p role="status" className="text-xl text-muted-foreground">
          {t('kids.map.loading')}
        </p>
      </Centered>
    )
  }

  if (map.isError) {
    return (
      <Centered>
        <div role="alert" className="flex flex-col items-center gap-4 text-center">
          <p className="text-xl">{t('kids.map.loadError')}</p>
          <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void map.refetch()}>
            {t('kids.map.retry')}
          </Button>
        </div>
      </Centered>
    )
  }

  const levels = map.data.levels
  if (levels.length === 0) {
    return (
      <Centered>
        {child && <ChildAvatar avatarKey={child.avatarKey} size="lg" />}
        <h1 className="font-heading text-4xl font-bold">
          {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
        </h1>
        <p className="text-xl text-muted-foreground">{t('kids.map.empty')}</p>
      </Centered>
    )
  }

  // A level that is no longer published sends the child back to the mainland.
  const at = levels.findIndex((level) => level.id === place.levelId)
  const avatarKey = child?.avatarKey ?? ''

  return (
    <div className="h-svh w-full">
      {at >= 0 && !atSea ? (
        <IslandScene
          key={levels[at]!.id}
          level={levels[at]!}
          index={at}
          avatarKey={avatarKey}
          lessonId={place.lessonId}
          greeting={greeting}
          onOpenLesson={(lessonId) => moveTo({ levelId: levels[at]!.id, lessonId })}
          onLeave={() => {
            moveTo({ levelId: levels[at]!.id, lessonId: null, atSea: true })
            setAtSea(true)
          }}
        />
      ) : (
        <>
          <h1 className="sr-only">{child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}</h1>
          <SeaScene
            levels={levels}
            at={at}
            avatarKey={avatarKey}
            greeting={greeting}
            onArrive={(level) => {
              moveTo({ levelId: level.id, lessonId: null })
              setAtSea(false)
            }}
          />
        </>
      )}
    </div>
  )
}

/** Loading, errors and the empty map: in the middle of the screen. */
function Centered({ children }: { children: ReactNode }) {
  return <div className="flex min-h-svh flex-col items-center justify-center gap-4 p-4 text-center">{children}</div>
}
