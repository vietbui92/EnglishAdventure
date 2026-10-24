import { Map as MapIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { usePlayLesson, type PlayLesson } from '@/areas/kids/play/api'
import { DiscoveryWalk } from '@/areas/kids/play/DiscoveryWalk'
import { GamePlayer } from '@/areas/kids/play/GamePlayer'
import { ReviewCards } from '@/areas/kids/play/ReviewCards'
import { ApiError } from '@/shared/api/problem-details'
import { useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'

/**
 * Playing one lesson (F4.8, F4.3, F4.4; PD-013): the discovery walk through the forest, the review cards with
 * speaking practice, then the games (`GamePlayer`), graded by the server one answer at a time (S4.1). A boss has no
 * walk or cards (owner, 2026-10-24): it starts with the fight. The walk fills the screen; the cards and games are
 * centred and grow on large screens (`kids-zoom`).
 */
export function LessonPlayPage() {
  const { t } = useTranslation()
  const { lessonId = '' } = useParams()
  const lesson = usePlayLesson(lessonId)

  if (lesson.isSuccess) {
    // Re-created for another lesson, so it starts again with the walk.
    return <LessonSteps key={lesson.data.id} lesson={lesson.data} />
  }

  return (
    <Page title={null}>
      {lesson.isPending ? (
        <p role="status" className="text-center text-xl text-muted-foreground">
          {t('kids.play.loading')}
        </p>
      ) : (
        <div role="alert" className="flex flex-col items-center gap-4 text-center">
          {lesson.error instanceof ApiError && lesson.error.kind === 'not-found' ? (
            <p className="text-xl">{t('kids.play.notFound')}</p>
          ) : lesson.error instanceof ApiError && lesson.error.code === 'lesson-locked' ? (
            // Opened from an old link: the lessons before it come first (S4.2).
            <p className="text-xl">{t('kids.play.locked')}</p>
          ) : (
            <>
              <p className="text-xl">{t('kids.play.loadError')}</p>
              <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void lesson.refetch()}>
                {t('kids.map.retry')}
              </Button>
            </>
          )}
        </div>
      )}
    </Page>
  )
}

type Step = 'discover' | 'review' | 'games'

function LessonSteps({ lesson }: { lesson: PlayLesson }) {
  const boss = lesson.kind === 'Boss'
  const child = useSession()?.user.childProfile
  const [step, setStep] = useState<Step>(!boss && lesson.learnCards.length > 0 ? 'discover' : 'games')

  if (step === 'discover') {
    return (
      <div className="relative h-svh w-full">
        <TopBar title={lesson.title} />
        <DiscoveryWalk cards={lesson.learnCards} avatarKey={child?.avatarKey ?? ''} onDone={() => setStep('review')} />
      </div>
    )
  }

  return (
    <Page title={lesson.title}>
      {step === 'review' ? (
        <ReviewCards cards={lesson.learnCards} onDone={() => setStep('games')} />
      ) : (
        <GamePlayer lesson={lesson} />
      )}
    </Page>
  )
}

/** "Back to the map" and the lesson's name, in the top left corner (the sound and exit buttons are on the right). */
function TopBar({ title }: { title: string | null }) {
  const { t } = useTranslation()
  return (
    <div className="absolute inset-x-0 top-0 z-30 flex items-center gap-3 p-3 pr-36 sm:p-4 sm:pr-64">
      <Button
        asChild
        variant="ghost"
        className="min-h-12 shrink-0 rounded-full bg-white/85 px-4 text-base shadow-md backdrop-blur hover:bg-white"
      >
        <Link to="/kids">
          <MapIcon aria-hidden className="size-5" />
          <span className="sr-only sm:not-sr-only">{t('kids.play.backToMap')}</span>
        </Link>
      </Button>
      {title && (
        <h1
          id="lesson-title"
          className="truncate rounded-full bg-white/85 px-5 py-2 font-heading text-lg font-bold shadow-md backdrop-blur sm:text-2xl"
        >
          {title}
        </h1>
      )}
    </div>
  )
}

/** The cards, the games and the waiting states: centred below the top bar, larger on large screens. */
function Page({ title, children }: { title: string | null; children: ReactNode }) {
  return (
    <div className="relative min-h-svh w-full">
      <TopBar title={title} />
      <section
        aria-labelledby={title ? 'lesson-title' : undefined}
        className="kids-zoom mx-auto flex min-h-svh w-full max-w-3xl flex-col items-center justify-center-safe gap-6 px-4 pt-20 pb-8 sm:pt-24"
      >
        {children}
      </section>
    </div>
  )
}
