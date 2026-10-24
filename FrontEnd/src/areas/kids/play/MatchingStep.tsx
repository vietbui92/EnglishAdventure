import { motion } from 'motion/react'
import { Check } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Burst, useNudges } from '@/areas/kids/adventure/effects'
import { playSound } from '@/areas/kids/adventure/sound'
import {
  useMatchPair,
  type AttemptView,
  type EarnedRewards,
  type PlayActivity,
  type PlayChoice,
} from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

interface MatchingStepProps {
  attemptId: string
  activity: PlayActivity
  attempt: AttemptView
  onAnswered: (attempt: AttemptView, rewards?: EarnedRewards | null) => void
  onNext: () => void
}

/**
 * A matching (F4.4, owner 2026-10-24): the child picks an English word, then its picture or meaning. The server checks
 * each match; a wrong one shakes and comes apart to be tried again (the right partner is never shown), a right one
 * stays, with a chime and confetti.
 */
export function MatchingStep({ attemptId, activity, attempt, onAnswered, onNext }: MatchingStepProps) {
  const { t } = useTranslation()
  const match = useMatchPair(attemptId)
  const [selected, setSelected] = useState<string | null>(null)
  const [missed, setMissed] = useState(false)
  const [celebrate, setCelebrate] = useState<string | null>(null)
  const { scope, pop, shake } = useNudges()
  const matching = activity.matching!
  const matchedLeft = new Set(attempt.matchedPairs.map((pair) => pair.leftId))
  const matchedRight = new Set(attempt.matchedPairs.map((pair) => pair.rightId))
  const done = matching.left.every((left) => matchedLeft.has(left.id))
  const pictures = matching.mode === 'WordToPicture'

  const pickRight = (right: PlayChoice) => {
    if (!selected) return
    setMissed(false)
    match.mutate(
      { leftId: selected, rightId: right.id },
      {
        onSuccess: (result) => {
          const pair = `[data-choice="${selected}"], [data-choice="${right.id}"]`
          setSelected(null)
          setMissed(!result.correct)
          onAnswered(result.attempt, result.rewards)
          if (result.correct) {
            const finished = matching.left.every(
              (left) => left.id === selected || result.attempt.matchedPairs.some((match) => match.leftId === left.id),
            )
            playSound(finished ? 'win' : 'correct')
            setCelebrate(right.id)
            pop(pair)
          } else {
            playSound('wrong')
            shake(pair)
          }
        },
      },
    )
  }

  return (
    <motion.div
      ref={scope}
      initial={{ opacity: 0, x: 60 }}
      animate={{ opacity: 1, x: 0 }}
      className="flex w-full flex-col items-center gap-6"
    >
      <p className="text-xl">{pictures ? t('kids.game.matchPictures') : t('kids.game.matchMeanings')}</p>

      <div className="grid w-full grid-cols-2 gap-4">
        <div role="group" aria-label={t('kids.game.words')} className="flex flex-col gap-3">
          {matching.left.map((left) => {
            const matched = matchedLeft.has(left.id)
            return (
              <button
                key={left.id}
                type="button"
                lang="en"
                disabled={matched || match.isPending}
                aria-pressed={selected === left.id}
                data-choice={left.id}
                onClick={() => {
                  playSound('tap')
                  setMissed(false)
                  setSelected(left.id)
                }}
                className={cn(
                  'relative flex min-h-20 items-center justify-center gap-2 rounded-3xl border-4 bg-card p-3 text-2xl transition-transform font-bold shadow-sm focus-visible:ring-4 focus-visible:ring-ring focus-visible:outline-none',
                  selected === left.id ? 'scale-105 border-primary' : 'border-transparent',
                  matched && 'border-green-500 bg-green-50 opacity-80',
                )}
              >
                {matched && <Check aria-hidden className="size-6 text-green-600" />}
                {left.text}
              </button>
            )
          })}
        </div>

        <div
          role="group"
          aria-label={pictures ? t('kids.game.pictures') : t('kids.game.meanings')}
          className="flex flex-col gap-3"
        >
          {matching.right.map((right, index) => {
            const matched = matchedRight.has(right.id)
            return (
              <button
                key={right.id}
                type="button"
                disabled={matched || !selected || match.isPending}
                data-choice={right.id}
                aria-label={right.text ?? t('kids.game.pictureOption', { number: index + 1 })}
                onClick={() => pickRight(right)}
                className={cn(
                  'relative flex min-h-20 items-center justify-center gap-2 rounded-3xl border-4 border-transparent bg-card p-3 text-xl font-semibold shadow-sm focus-visible:ring-4 focus-visible:ring-ring focus-visible:outline-none',
                  matched && 'border-green-500 bg-green-50 opacity-80',
                )}
              >
                {celebrate === right.id && <Burst />}
                {matched && <Check aria-hidden className="size-6 text-green-600" />}
                {pictures ? <KidsPicture url={right.imageUrl} alt="" className="size-24" /> : right.text}
              </button>
            )
          })}
        </div>
      </div>

      <div className="flex min-h-16 flex-col items-center gap-3 text-center" aria-live="polite">
        {match.isError ? (
          <p role="alert" className="text-lg">
            {t('kids.game.answerError')}
          </p>
        ) : done ? (
          <>
            <p className="text-2xl font-bold">{t('kids.game.allMatched')}</p>
            <Button className="min-h-14 rounded-2xl px-10 text-xl" onClick={onNext}>
              {t('kids.game.next')}
            </Button>
          </>
        ) : missed ? (
          <p className="text-2xl font-bold">{t('kids.game.tryAgain')}</p>
        ) : (
          <p className="text-lg text-muted-foreground">
            {selected ? t('kids.game.nowPickPartner') : t('kids.game.pickWordFirst')}
          </p>
        )}
      </div>
    </motion.div>
  )
}
