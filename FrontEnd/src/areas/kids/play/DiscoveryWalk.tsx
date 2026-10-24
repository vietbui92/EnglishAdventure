import { motion, useMotionValue, useReducedMotion, useTransform, type TargetAndTransition } from 'motion/react'
import { ArrowRight, Footprints, Layers, Music } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Burst } from '@/areas/kids/adventure/effects'
import {
  FarCanopy,
  Fireflies,
  ForestFloor,
  HidingBush,
  LightRays,
  MiddleTrees,
  Mist,
  NearLeaves,
  Pond,
} from '@/areas/kids/adventure/forest'
import { across, down, follow, Layer } from '@/areas/kids/adventure/Layer'
import { Explorer } from '@/areas/kids/adventure/scenery'
import { playClip, playSound, speak, startForest, unlockSound } from '@/areas/kids/adventure/sound'
import { Stage } from '@/areas/kids/adventure/Stage'
import { glide, pause } from '@/areas/kids/adventure/track'
import type { LearnCard } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { ListenButton } from '@/areas/kids/play/ListenButton'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

// The path runs along the forest floor; words wait beside it, one every SPACING units.
const PATH = { top: 380, bottom: 450 }
const GROUND = 425
const SPACING = 700
const spotOf = (index: number) => 700 + index * SPACING
const standOf = (index: number) => spotOf(index) - 260
const START = 80

/**
 * How a word's picture moves (S3.9, PD-013). Each is a loop the app plays on the picture itself, so the Admin only
 * picks a movement. With "reduce motion" the pictures stand still (MotionConfig in KidsLayout).
 */
const movements: Record<string, TargetAndTransition> = {
  Still: { scale: [1, 1.05, 1], transition: { duration: 3, repeat: Infinity, ease: 'easeInOut' } },
  Walk: {
    x: ['-60%', '60%', '-60%'],
    y: ['0%', '-5%', '0%', '-5%', '0%', '-5%', '0%', '-5%', '0%'],
    scaleX: [1, 1, -1, -1, 1],
    transition: {
      duration: 6,
      repeat: Infinity,
      ease: 'linear',
      scaleX: { duration: 6, repeat: Infinity, times: [0, 0.49, 0.5, 0.99, 1] },
    },
  },
  Hop: {
    x: ['-40%', '-40%', '0%', '0%', '40%', '40%', '0%', '0%', '-40%'],
    y: ['0%', '-45%', '0%', '-45%', '0%', '-45%', '0%', '-45%', '0%'],
    transition: { duration: 3.6, repeat: Infinity, ease: 'easeInOut' },
  },
  Fly: {
    x: ['-70%', '0%', '70%', '0%', '-70%'],
    y: ['0%', '-25%', '0%', '25%', '0%'],
    rotate: [-10, 6, 10, -6, -10],
    transition: { duration: 5, repeat: Infinity, ease: 'easeInOut' },
  },
  Swim: {
    x: ['-50%', '50%', '-50%'],
    y: ['0%', '-8%', '0%', '8%', '0%'],
    rotate: [0, 8, 0, -8, 0],
    scaleX: [1, 1, -1, -1, 1],
    transition: {
      duration: 5,
      repeat: Infinity,
      ease: 'easeInOut',
      scaleX: { duration: 5, repeat: Infinity, times: [0, 0.49, 0.5, 0.99, 1] },
    },
  },
  Crawl: {
    x: ['-25%', '25%', '-25%'],
    scaleX: [1, 1.12, 1, 1.12, 1, 1.12, 1],
    scaleY: [1, 0.9, 1, 0.9, 1, 0.9, 1],
    transition: { duration: 9, repeat: Infinity, ease: 'easeInOut' },
  },
  Wiggle: { rotate: [-10, 10, -10], transition: { duration: 1.2, repeat: Infinity, ease: 'easeInOut' } },
}

/** Where a word waits: a flyer up among the branches, a swimmer in its pond, the others on the ground. */
const heightOf = (movement: string) => (movement === 'Fly' ? 250 : movement === 'Swim' ? GROUND + 30 : GROUND)

interface DiscoveryWalkProps {
  cards: LearnCard[]
  avatarKey: string
  /** After the last word: on to the review cards. */
  onDone: () => void
}

/**
 * The discovery walk (F4.8, PD-013; owner 2026-10-24: a walk into a mysterious forest). The explorer walks into the
 * forest along its path. Each word hides in a bush with glowing eyes; when the explorer comes close the bush
 * rustles, parts, and the word comes out moving its own way (a cat walks, a frog hops, a bird flies among the branches,
 * a fish swims in its pond). Its sound plays if it has one, its name appears and is read aloud. "Walk on" goes deeper;
 * after the last word the cards come back for review. Wind, crickets and birds play while the walk is open.
 */
export function DiscoveryWalk({ cards, avatarKey, onDone }: DiscoveryWalkProps) {
  const { t } = useTranslation()
  const still = useReducedMotion() ?? false
  const width = spotOf(cards.length - 1) + 500

  const heroX = useMotionValue(START)
  const heroLeft = useTransform(heroX, (x) => across(x, width))
  const camera = useTransform(heroX, (x) => follow(x, width, 260))
  const [walking, setWalking] = useState(false)
  const [rustling, setRustling] = useState<number | null>(null)
  // Words before `revealed` are out of their bushes; `met` is the one the explorer is with.
  const [revealed, setRevealed] = useState(0)
  const [met, setMet] = useState<number | null>(null)

  useEffect(() => startForest(), [])

  const meet = async (index: number) => {
    const card = cards[index]!
    unlockSound()
    setMet(null)
    setWalking(true)
    const distance = Math.abs(standOf(index) - heroX.get())
    const steps = still ? undefined : setInterval(() => playSound('step'), 320)
    await glide(heroX, standOf(index), Math.max(0.8, distance / 200), still, 'linear')
    clearInterval(steps)
    setWalking(false)

    // Something is hiding…
    setRustling(index)
    playSound('rustle')
    await pause(0.6, still)
    playSound('rustle')
    await pause(0.6, still)
    setRustling(null)

    setRevealed(index + 1)
    setMet(index)
    playSound('reveal')
    if (card.soundUrl) await playClip(card.soundUrl)
    speak(card.english, { auto: true })
  }

  const card = met === null ? null : cards[met]!
  const last = met === cards.length - 1
  const busy = walking || rustling !== null
  const started = walking || rustling !== null || met !== null

  return (
    <Stage
      minWidth={900}
      horizon={PATH.top - 40}
      sky={['#0d2e2b', '#1b4a3c']}
      ground={['#1f4a32', '#10291c']}
      hud={
        <>
          {/* The forest is darker at the edges. */}
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_center,transparent_45%,rgba(0,0,0,0.5))]" />
          <p
            aria-live="polite"
            className="absolute top-20 left-1/2 w-max max-w-[90%] -translate-x-1/2 rounded-full bg-black/45 px-5 py-2 text-center text-base font-bold text-white backdrop-blur sm:top-24 sm:text-xl"
          >
            {rustling !== null
              ? t('kids.discover.hiding')
              : walking
                ? t('kids.discover.walking')
                : met === null
                  ? t('kids.discover.intro')
                  : t('kids.discover.count', { number: met + 1, count: cards.length })}
          </p>

          <div className="absolute inset-x-0 bottom-0 flex flex-col items-center gap-3 p-3 sm:p-5">
            {!started && (
              <Button className="kids-pulse min-h-16 rounded-2xl px-10 text-2xl shadow-lg" onClick={() => void meet(0)}>
                <Footprints aria-hidden className="size-6" />
                {t('kids.discover.start')}
              </Button>
            )}
            {card && (
              <motion.article
                key={met}
                aria-label={card.english}
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                className="flex w-full max-w-4xl flex-wrap items-center justify-center gap-x-5 gap-y-2 rounded-3xl bg-white/90 px-5 py-3 text-center shadow-xl backdrop-blur"
              >
                <p lang="en" className="font-heading text-4xl font-bold sm:text-5xl">
                  {card.english}
                </p>
                <p className="text-xl text-muted-foreground sm:text-2xl">{card.meaning}</p>
                <span className="flex gap-2">
                  <ListenButton text={card.english} />
                  {card.soundUrl && (
                    <Button
                      variant="secondary"
                      className="min-h-12 rounded-full px-5 text-base"
                      onClick={() => void playClip(card.soundUrl!)}
                    >
                      <Music aria-hidden className="size-5" />
                      {t('kids.discover.sound')}
                    </Button>
                  )}
                </span>
                {last ? (
                  <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={onDone}>
                    <Layers aria-hidden className="size-5" />
                    {t('kids.discover.toReview')}
                  </Button>
                ) : (
                  <Button className="min-h-14 rounded-2xl px-8 text-xl" onClick={() => void meet(met! + 1)}>
                    {t('kids.discover.next')}
                    <ArrowRight aria-hidden className="size-5" />
                  </Button>
                )}
              </motion.article>
            )}
            {!last && (
              <Button
                variant="ghost"
                className="min-h-12 rounded-full bg-black/30 px-4 text-base text-white hover:bg-black/50 hover:text-white"
                disabled={busy}
                onClick={onDone}
              >
                {t('kids.discover.skip')}
              </Button>
            )}
          </div>
        </>
      }
    >
      <div aria-hidden className={cn('absolute inset-0', walking && 'kids-trek')}>
        <Layer camera={camera} world={width} depth={0.1} drawing={(from, to) => <FarCanopy from={from} to={to} />} />
        <Layer camera={camera} world={width} depth={0.2} drawing={(from, to) => <LightRays from={from} to={to} />} />
        <Layer camera={camera} world={width} depth={0.4} drawing={(from, to) => <MiddleTrees from={from} to={to} />} />
        <Layer
          camera={camera}
          world={width}
          depth={0.6}
          drawing={(from, to) => <Mist from={from} to={to} y={PATH.top - 20} />}
        />

        <Layer
          camera={camera}
          world={width}
          depth={1}
          drawing={(from, to) => (
            <>
              <ForestFloor from={from} to={to} path={PATH} />
              {cards.map(
                (word, index) =>
                  word.movement === 'Swim' && <Pond key={word.english} x={spotOf(index)} y={GROUND + 20} />,
              )}
            </>
          )}
        >
          {cards.map((word, index) => {
            const out = index < revealed
            return (
              <div
                key={word.english}
                className="absolute w-[19cqw] -translate-x-1/2 -translate-y-full"
                style={{ left: across(spotOf(index), width), top: down(heightOf(word.movement)) }}
              >
                {met === index && (
                  <motion.span
                    initial={{ scale: 0, y: 20 }}
                    animate={{ scale: 1, y: 0 }}
                    transition={{ type: 'spring', stiffness: 400, damping: 14 }}
                    lang="en"
                    className="absolute -top-[8cqw] left-1/2 z-10 -translate-x-1/2 rounded-[1.6cqw] border-[0.4cqw] border-white bg-[#f59e0b] px-[2cqw] py-[0.5cqw] font-heading text-[4.2cqw] leading-tight font-bold whitespace-nowrap text-white shadow-lg"
                  >
                    {word.english}
                  </motion.span>
                )}
                <div className="relative aspect-square w-full">
                  {out && (
                    <motion.div
                      initial={{ scale: 0, opacity: 0 }}
                      animate={{ scale: 1, opacity: 1 }}
                      transition={{ type: 'spring', stiffness: 220, damping: 14, delay: still ? 0 : 0.25 }}
                      className="absolute inset-0"
                    >
                      {met === index && <Burst />}
                      <motion.div animate={movements[word.movement] ?? movements.Still} className="size-full">
                        <KidsPicture
                          url={word.imageUrl}
                          alt=""
                          className="size-full drop-shadow-[0_0_1.2cqw_rgba(255,245,157,0.55)]"
                        />
                      </motion.div>
                    </motion.div>
                  )}
                  {/* Each word waits in a bush, which parts when it comes out (and then stays out of the way). */}
                  <div className="pointer-events-none absolute inset-0">
                    <HidingBush rustling={rustling === index} open={out} />
                  </div>
                </div>
              </div>
            )
          })}

          <motion.div
            className="absolute -translate-x-1/2 -translate-y-full"
            style={{ left: heroLeft, top: down(GROUND + 25) }}
          >
            <Explorer avatarKey={avatarKey} walking={walking} className="size-[8cqw]" />
          </motion.div>
        </Layer>

        <Layer camera={camera} world={width} depth={1.2} drawing={(from, to) => <Fireflies from={from} to={to} />} />
        <Layer
          camera={camera}
          world={width}
          depth={1.6}
          className="pointer-events-none"
          drawing={(from, to) => <NearLeaves from={from} to={to} />}
        />
      </div>
    </Stage>
  )
}
