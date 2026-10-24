import { useEffect, useState } from 'react'
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
  const [atSea, setAtSea] = useState(false)

  // The sea and the music while the map is open.
  useEffect(() => startAmbience(), [])

  const moveTo = (next: AdventurePlace) => {
    setPlace(next)
    savePlace(child?.id, next)
  }

  const greeting = (
    <span className="flex items-center gap-[1cqw] rounded-full bg-white/90 py-[0.6cqw] pr-[2cqw] pl-[0.6cqw] shadow-md">
      {child && <ChildAvatar avatarKey={child.avatarKey} size="sm" className="size-[5cqw] text-[3cqw]" />}
      <span className="font-heading text-[2.2cqw] font-bold">
        {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
      </span>
    </span>
  )

  if (map.isPending) {
    return (
      <p role="status" className="text-xl text-muted-foreground">
        {t('kids.map.loading')}
      </p>
    )
  }

  if (map.isError) {
    return (
      <div role="alert" className="flex flex-col items-center gap-4 text-center">
        <p className="text-xl">{t('kids.map.loadError')}</p>
        <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void map.refetch()}>
          {t('kids.map.retry')}
        </Button>
      </div>
    )
  }

  const levels = map.data.levels
  if (levels.length === 0) {
    return (
      <div className="flex flex-col items-center gap-4 text-center">
        {child && <ChildAvatar avatarKey={child.avatarKey} size="lg" />}
        <h1 className="font-heading text-4xl font-bold">
          {child ? t('kids.greeting', { nickname: child.nickname }) : t('kids.title')}
        </h1>
        <p className="text-xl text-muted-foreground">{t('kids.map.empty')}</p>
      </div>
    )
  }

  // A level that is no longer published sends the child back to the mainland.
  const at = levels.findIndex((level) => level.id === place.levelId)
  const avatarKey = child?.avatarKey ?? ''

  return (
    <div className="flex w-[min(100%,calc(78svh*5/3))] flex-col items-center">
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
            moveTo({ levelId: levels[at]!.id, lessonId: null })
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
