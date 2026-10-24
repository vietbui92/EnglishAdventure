import { motion, useAnimate, useReducedMotion } from 'motion/react'
import { RotateCcw, Star, Swords } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Burst } from '@/areas/kids/adventure/effects'
import { playSound } from '@/areas/kids/adventure/sound'
import { gameSteps, nextStep, useStartAttempt, type AttemptView, type PlayLesson } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { MatchingStep } from '@/areas/kids/play/MatchingStep'
import { QuizStep } from '@/areas/kids/play/QuizStep'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/**
 * The games of a lesson or boss (F4.3, F4.4). It starts (or resumes, PD-012) the attempt, goes through each quiz
 * question and each matching in order, skipping what is already done, and shows the result with its stars at the end.
 * A boss shows a health bar that drops with every right answer (display only: the server grades, PD-012); it
 * shakes and flashes at each hit. The result's stars pop in one by one, with a fanfare when there is at least one.
 */
export function GamePlayer({ lesson }: { lesson: PlayLesson }) {
  const { t } = useTranslation()
  const start = useStartAttempt(lesson.id)
  const [attempt, setAttempt] = useState<AttemptView | null>(null)
  const [step, setStep] = useState(0)
  const steps = gameSteps(lesson.activities)
  const boss = lesson.kind === 'Boss'

  const begin = () =>
    start.mutate(undefined, {
      onSuccess: (started) => {
        setAttempt(started)
        setStep(nextStep(steps, started))
      },
    })

  if (!attempt) {
    return (
      <div className="flex flex-col items-center gap-4 text-center">
        {boss && (
          <>
            <motion.div
              animate={{ y: [0, -10, 0], rotate: [0, -2, 2, 0] }}
              transition={{ duration: 2.4, repeat: Infinity, ease: 'easeInOut' }}
            >
              <KidsPicture url={lesson.bossImageUrl} alt={lesson.title} className="size-56" />
            </motion.div>
            <p className="flex items-center gap-2 text-2xl font-bold">
              <Swords aria-hidden className="size-7" />
              {t('kids.play.bossIntro', { name: lesson.title })}
            </p>
          </>
        )}
        {start.isError && (
          <p role="alert" className="text-lg">
            {t('kids.game.startError')}
          </p>
        )}
        <Button
          className="kids-pulse min-h-16 rounded-2xl px-10 text-2xl"
          disabled={start.isPending}
          onClick={() => {
            playSound('tap')
            begin()
          }}
        >
          {boss ? <Swords aria-hidden className="size-6" /> : null}
          {boss ? t('kids.game.fight') : t('kids.game.start')}
        </Button>
      </div>
    )
  }

  // Past the last screen (or resumed with everything done): the result.
  const current = steps[step]

  return (
    <div className="flex w-full flex-col items-center gap-6">
      {boss && <BossHealth lesson={lesson} attempt={attempt} />}

      {current ? (
        current.kind === 'question' ? (
          <QuizStep
            key={current.question.id}
            attemptId={attempt.attemptId}
            question={current.question}
            number={current.number}
            count={current.count}
            answered={attempt.questions.find((answered) => answered.questionId === current.question.id)}
            onAnswered={setAttempt}
            onNext={() => setStep(nextStep(steps, attempt, step + 1))}
          />
        ) : (
          <MatchingStep
            key={current.activity.id}
            attemptId={attempt.attemptId}
            activity={current.activity}
            attempt={attempt}
            onAnswered={setAttempt}
            onNext={() => setStep(nextStep(steps, attempt, step + 1))}
          />
        )
      ) : (
        <ResultScreen
          lesson={lesson}
          attempt={attempt}
          onReplay={() => {
            setAttempt(null)
            setStep(0)
            begin()
          }}
        />
      )}
    </div>
  )
}

/** The boss's health: full at the start, one notch less for every right answer or match. */
function BossHealth({ lesson, attempt }: { lesson: PlayLesson; attempt: AttemptView }) {
  const { t } = useTranslation()
  const total = Number(attempt.totalItems)
  const hits = attempt.questions.filter((answered) => answered.correct).length + attempt.matchedPairs.length
  const left = Math.max(0, total - hits)
  const [scope, animate] = useAnimate<HTMLDivElement>()
  const still = useReducedMotion()
  const before = useRef(left)

  // Each drop is a hit on the boss.
  useEffect(() => {
    if (left < before.current) {
      playSound('hit')
      if (!still) {
        void animate(
          '[data-boss]',
          { x: [0, -10, 10, -6, 6, 0], filter: ['brightness(1)', 'brightness(2)', 'brightness(1)'] },
          { duration: 0.5 },
        )
      }
    }
    before.current = left
  }, [left, animate, still])

  return (
    <div ref={scope} className="flex w-full items-center gap-3 rounded-3xl bg-card p-3 shadow-sm">
      <div data-boss>
        <KidsPicture url={lesson.bossImageUrl} alt={lesson.title} className="size-16" />
      </div>
      <div className="flex flex-1 flex-col gap-1">
        <span className="font-bold">{lesson.title}</span>
        <div
          role="meter"
          aria-label={t('kids.game.bossHealth', { name: lesson.title })}
          aria-valuemin={0}
          aria-valuemax={total}
          aria-valuenow={left}
          className="h-5 w-full overflow-hidden rounded-full bg-muted"
        >
          <div
            className="h-full rounded-full bg-gradient-to-r from-red-600 to-orange-400 transition-all duration-500"
            style={{ width: `${total === 0 ? 0 : (left / total) * 100}%` }}
          />
        </div>
      </div>
    </div>
  )
}

/** The end of an attempt: the stars (PD-011), the score, and playing again. */
function ResultScreen({
  lesson,
  attempt,
  onReplay,
}: {
  lesson: PlayLesson
  attempt: AttemptView
  onReplay: () => void
}) {
  const { t } = useTranslation()
  const result = attempt.result
  const stars = Number(result?.stars ?? 0)
  const boss = lesson.kind === 'Boss'
  const message = boss
    ? stars > 0
      ? t('kids.game.bossDefeated', { name: lesson.title })
      : t('kids.game.bossStands', { name: lesson.title })
    : stars === 3
      ? t('kids.game.perfect')
      : stars > 0
        ? t('kids.game.wellDone')
        : t('kids.game.keepTrying')

  // A star sound as each earned star lands, then the fanfare.
  useEffect(() => {
    const timers = Array.from({ length: stars }, (_, index) => setTimeout(() => playSound('star'), 400 + index * 350))
    timers.push(setTimeout(() => playSound(stars > 0 ? 'win' : 'wrong'), 400 + stars * 350))
    return () => timers.forEach(clearTimeout)
  }, [stars])

  return (
    <motion.div
      initial={{ scale: 0.8, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ type: 'spring', stiffness: 260, damping: 18 }}
      className="relative flex flex-col items-center gap-6 rounded-3xl bg-card p-8 text-center shadow-sm"
      aria-live="polite"
    >
      {stars > 0 && <Burst big />}
      <p className="font-heading text-3xl font-bold">{message}</p>
      <div role="img" aria-label={t('kids.game.stars', { count: stars })} className="flex gap-2">
        {[1, 2, 3].map((position) => (
          <motion.span
            key={position}
            initial={{ scale: 0, rotate: -40 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: 'spring', stiffness: 300, damping: 12, delay: 0.3 + (position - 1) * 0.35 }}
          >
            <Star
              aria-hidden
              className={cn(
                'size-16',
                position <= stars ? 'fill-yellow-400 text-yellow-500 drop-shadow' : 'text-muted-foreground',
              )}
            />
          </motion.span>
        ))}
      </div>
      {result && (
        <p className="text-xl">
          {t('kids.game.score', {
            correct: Number(result.correctItems),
            total: Number(result.totalItems),
            percent: Number(result.scorePercent),
          })}
        </p>
      )}
      <div className="flex flex-wrap justify-center gap-3">
        <Button variant="outline" className="min-h-14 rounded-2xl px-8 text-xl" onClick={onReplay}>
          <RotateCcw aria-hidden className="size-5" />
          {t('kids.game.replay')}
        </Button>
        <Button asChild className="min-h-14 rounded-2xl px-8 text-xl">
          <Link to="/kids">{t('kids.play.backToMap')}</Link>
        </Button>
      </div>
    </motion.div>
  )
}
