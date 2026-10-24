import { motion } from 'motion/react'
import { ArrowLeft, ArrowRight, Mic, Play } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Burst } from '@/areas/kids/adventure/effects'
import { playSound } from '@/areas/kids/adventure/sound'
import type { LearnCard } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { ListenButton } from '@/areas/kids/play/ListenButton'
import { canHearSpeech, useSpeechPractice } from '@/areas/kids/play/speech'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

/**
 * The review cards (S3.7, PD-013), after the discovery walk: one word card at a time, in order, turning over as it
 * comes. The word can be heard, and where the browser can hear the child, said aloud for practice. Not scored.
 */
export function ReviewCards({ cards, onDone }: { cards: LearnCard[]; onDone: () => void }) {
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

      <motion.article
        key={index}
        aria-label={card.english}
        initial={{ rotateY: 80, opacity: 0 }}
        animate={{ rotateY: 0, opacity: 1 }}
        transition={{ type: 'spring', stiffness: 200, damping: 20 }}
        className="relative flex w-full flex-col items-center gap-4 rounded-3xl bg-card p-6 text-center shadow-sm"
      >
        <KidsPicture url={card.imageUrl} alt={card.english} className="size-56 sm:size-64" />
        <div className="flex items-center gap-3">
          <p lang="en" className="font-heading text-5xl font-bold">
            {card.english}
          </p>
          <ListenButton text={card.english} />
        </div>
        <p className="text-2xl text-muted-foreground">{card.meaning}</p>
        {/* A new practice for each card. */}
        {canHearSpeech() && <SpeakPractice key={index} word={card.english} />}
      </motion.article>

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
          <Button
            className="min-h-14 rounded-2xl px-8 text-xl"
            onClick={() => {
              playSound('tap')
              setIndex(index + 1)
            }}
          >
            {t('kids.play.next')}
            <ArrowRight aria-hidden className="size-5" />
          </Button>
        )}
      </div>
      {/* A child coming back to an unfinished lesson can go straight to where they stopped (PD-012). */}
      {!last && (
        <Button variant="ghost" className="min-h-12 px-4 text-base" onClick={onDone}>
          {t('kids.play.skipCards')}
        </Button>
      )}
    </div>
  )
}

/** "Say it!": the child taps the microphone and says the word; a right one gets a cheer, otherwise try again. */
function SpeakPractice({ word }: { word: string }) {
  const { t } = useTranslation()
  const { practice, listen } = useSpeechPractice(word)

  useEffect(() => {
    if (practice.state === 'right') playSound('correct')
    if (practice.state === 'wrong') playSound('wrong')
  }, [practice.state])

  const listening = practice.state === 'listening'

  return (
    <div className="relative flex w-full flex-col items-center gap-3 border-t pt-4">
      {practice.state === 'right' && <Burst />}
      <Button
        className={cn('min-h-14 rounded-full px-8 text-xl', listening && 'kids-pulse')}
        variant={practice.state === 'right' ? 'secondary' : 'default'}
        disabled={listening}
        onClick={() => {
          playSound('tap')
          listen()
        }}
      >
        <Mic aria-hidden className="size-6" />
        {listening ? t('kids.speak.listening') : t('kids.speak.say')}
      </Button>
      <p aria-live="polite" className={cn('text-lg', practice.state === 'right' && 'font-bold text-green-600')}>
        {practice.state === 'right'
          ? t('kids.speak.right')
          : practice.state === 'wrong'
            ? t('kids.speak.wrong', { heard: practice.heard })
            : practice.state === 'silent'
              ? t('kids.speak.silent')
              : practice.state === 'blocked'
                ? t('kids.speak.blocked')
                : listening
                  ? ''
                  : t('kids.speak.hint')}
      </p>
    </div>
  )
}
