import { ArrowLeft, ArrowRight, Map as MapIcon, Play, Swords } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { usePlayLesson, type LearnCard, type PlayLesson } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/components/ui/button'

/**
 * Playing one lesson (F4.6): the learn step's word cards one at a time, then the games. The games themselves come with
 * F4.3 and F4.4, once answers can be sent (S4.1). A boss has no learn step (owner, 2026-10-24): it starts with the fight.
 */
export function LessonPlayPage() {
  const { t } = useTranslation()
  const { lessonId = '' } = useParams()
  const lesson = usePlayLesson(lessonId)

  return (
    <div className="flex w-full max-w-2xl flex-col gap-6">
      <Button asChild variant="ghost" className="min-h-12 self-start px-4 text-base">
        <Link to="/kids">
          <MapIcon aria-hidden className="size-5" />
          {t('kids.play.backToMap')}
        </Link>
      </Button>

      {lesson.isPending ? (
        <p role="status" className="text-center text-xl text-muted-foreground">
          {t('kids.play.loading')}
        </p>
      ) : lesson.isError ? (
        <div role="alert" className="flex flex-col items-center gap-4 text-center">
          {lesson.error instanceof ApiError && lesson.error.kind === 'not-found' ? (
            <p className="text-xl">{t('kids.play.notFound')}</p>
          ) : (
            <>
              <p className="text-xl">{t('kids.play.loadError')}</p>
              <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void lesson.refetch()}>
                {t('kids.map.retry')}
              </Button>
            </>
          )}
        </div>
      ) : (
        // Re-created for another lesson, so the learn step starts again at its first card.
        <LessonSteps key={lesson.data.id} lesson={lesson.data} />
      )}
    </div>
  )
}

function LessonSteps({ lesson }: { lesson: PlayLesson }) {
  const { t } = useTranslation()
  const boss = lesson.kind === 'Boss'
  const [learning, setLearning] = useState(!boss && lesson.learnCards.length > 0)

  return (
    <section aria-labelledby="lesson-title" className="flex flex-col items-center gap-6">
      <h1 id="lesson-title" className="text-center font-heading text-3xl font-bold">
        {lesson.title}
      </h1>
      {learning ? (
        <LearnStep cards={lesson.learnCards} onDone={() => setLearning(false)} />
      ) : boss ? (
        <div className="flex flex-col items-center gap-4 text-center">
          <KidsPicture url={lesson.bossImageUrl} alt={lesson.title} className="size-56" />
          <p className="flex items-center gap-2 text-2xl font-bold">
            <Swords aria-hidden className="size-7" />
            {t('kids.play.bossIntro', { name: lesson.title })}
          </p>
          <GamesComingSoon />
        </div>
      ) : (
        <GamesComingSoon />
      )}
    </section>
  )
}

/** The learn step (S3.7, PD-011): one word card at a time, in order. Not scored. */
function LearnStep({ cards, onDone }: { cards: LearnCard[]; onDone: () => void }) {
  const { t } = useTranslation()
  const [index, setIndex] = useState(0)
  const card = cards[index]
  const last = index === cards.length - 1

  if (!card) return null

  return (
    <div className="flex w-full flex-col items-center gap-6">
      <p className="text-lg font-semibold text-muted-foreground" aria-live="polite">
        {t('kids.play.cardCount', { number: index + 1, count: cards.length })}
      </p>

      <article
        aria-label={card.english}
        className="flex w-full flex-col items-center gap-4 rounded-3xl bg-card p-6 text-center shadow-sm"
      >
        <KidsPicture url={card.imageUrl} alt={card.english} className="size-56 sm:size-64" />
        <p lang="en" className="font-heading text-5xl font-bold">
          {card.english}
        </p>
        <p className="text-2xl text-muted-foreground">{card.meaning}</p>
      </article>

      <div className="flex w-full flex-wrap justify-between gap-3">
        <Button
          variant="outline"
          className="min-h-14 rounded-2xl px-6 text-lg"
          disabled={index === 0}
          onClick={() => setIndex(index - 1)}
        >
          <ArrowLeft aria-hidden className="size-5" />
          {t('kids.play.previous')}
        </Button>
        {last ? (
          <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={onDone}>
            <Play aria-hidden className="size-5" />
            {t('kids.play.toGames')}
          </Button>
        ) : (
          <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => setIndex(index + 1)}>
            {t('kids.play.next')}
            <ArrowRight aria-hidden className="size-5" />
          </Button>
        )}
      </div>
    </div>
  )
}

/** Until the quiz and matching players exist (F4.3, F4.4). */
function GamesComingSoon() {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col items-center gap-4 rounded-3xl bg-card p-6 text-center shadow-sm">
      <p className="text-2xl font-bold">{t('kids.play.gamesSoon')}</p>
      <p className="text-muted-foreground">{t('kids.play.gamesSoonHint')}</p>
      <Button asChild className="min-h-14 rounded-2xl px-8 text-xl">
        <Link to="/kids">{t('kids.play.backToMap')}</Link>
      </Button>
    </div>
  )
}
