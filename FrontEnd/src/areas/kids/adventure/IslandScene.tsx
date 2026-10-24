import { motion, useMotionValue, useReducedMotion, useTransform } from 'motion/react'
import { Lock, Sailboat, Star, Swords } from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { useNudges } from '@/areas/kids/adventure/effects'
import { levelLabel } from '@/areas/kids/adventure/labels'
import { Boat, Decor, Explorer, themeOf, WaveRows } from '@/areas/kids/adventure/scenery'
import { playSound, unlockSound } from '@/areas/kids/adventure/sound'
import { Stage } from '@/areas/kids/adventure/Stage'
import { glide, pause, smooth, svgPath, track, type Point } from '@/areas/kids/adventure/track'
import { type MapLevel, type StopState } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

const VIEW = 1000
const HEIGHT = 600

// The island's footpath, from the boat at the bottom left up to the boss's lair at the top right.
const footpath = smooth([
  { x: 175, y: 520 },
  { x: 300, y: 490 },
  { x: 450, y: 515 },
  { x: 600, y: 480 },
  { x: 730, y: 430 },
  { x: 660, y: 350 },
  { x: 500, y: 345 },
  { x: 360, y: 310 },
  { x: 330, y: 235 },
  { x: 450, y: 190 },
  { x: 600, y: 205 },
  { x: 720, y: 185 },
  { x: 805, y: 170 },
])
const trail = track(footpath)

// Trees beside the path, never on it.
const trees: (Point & { size: number })[] = [
  { x: 120, y: 420, size: 1.3 },
  { x: 260, y: 400, size: 1 },
  { x: 540, y: 410, size: 1.1 },
  { x: 860, y: 420, size: 1.3 },
  { x: 220, y: 280, size: 0.9 },
  { x: 560, y: 280, size: 0.9 },
  { x: 900, y: 300, size: 1 },
  { x: 380, y: 140, size: 0.8 },
]

interface Stop {
  id: string
  /** Where on the footpath, from 0 (the boat) to 1 (the boss). */
  at: number
  boss: boolean
  title: string
  number: number
  imageUrl: string | null
  state: StopState
  stars: number
}

function stopsOf(level: MapLevel): Stop[] {
  const lessons = level.lessons.map((lesson, index) => ({
    id: lesson.id,
    at: ((index + 1) / (level.lessons.length + 1)) * 0.88,
    boss: false,
    title: lesson.title,
    number: index + 1,
    imageUrl: null,
    state: lesson.state,
    stars: Number(lesson.bestStars),
  }))
  const boss = level.boss
    ? [
        {
          id: level.boss.id,
          at: 1,
          boss: true,
          title: level.boss.name,
          number: 0,
          imageUrl: level.boss.imageUrl,
          state: level.boss.state,
          stars: Number(level.boss.bestStars),
        },
      ]
    : []
  return [...lessons, ...boss]
}

const left = (x: number) => `${(x / VIEW) * 100}%`
const top = (y: number) => `${(y / HEIGHT) * 100}%`

interface IslandSceneProps {
  level: MapLevel
  index: number
  avatarKey: string
  /** The lesson the character last went to on this island; it starts there. */
  lessonId: string | null
  greeting: ReactNode
  onOpenLesson: (lessonId: string) => void
  onLeave: () => void
}

/**
 * A level's island up close (owner 2026-10-24): a footpath with a signpost for each lesson, in order, and the boss's
 * lair at its end. The child taps a stop; the character walks the path to it and the lesson opens. "Back to the boat"
 * walks back down and sails off to the sea. Stops show where the child is (S4.2): completed ones are green with their
 * stars, the next one glows, locked ones are grey and say what opens them instead of opening.
 */
export function IslandScene({ level, index, avatarKey, lessonId, greeting, onOpenLesson, onLeave }: IslandSceneProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const still = useReducedMotion() ?? false
  const theme = themeOf(index)
  const stops = useMemo(() => stopsOf(level), [level])

  const progress = useMotionValue(stops.find((stop) => stop.id === lessonId)?.at ?? 0)
  const heroLeft = useTransform(progress, (fraction) => left(trail.at(fraction).x))
  const heroTop = useTransform(progress, (fraction) => top(trail.at(fraction).y))
  const [walking, setWalking] = useState<Stop | 'boat' | null>(null)
  const [facing, setFacing] = useState(1)
  const [notice, setNotice] = useState<string | null>(null)
  const { scope, shake } = useNudges()

  const walkTo = async (fraction: number) => {
    const from = progress.get()
    const distance = Math.abs(fraction - from) * trail.length
    if (distance > 1) {
      setFacing(trail.at(fraction).x >= trail.at(from).x ? 1 : -1)
      const steps = still ? undefined : setInterval(() => playSound('step'), 300)
      await glide(progress, fraction, Math.max(0.5, distance / 200), still, 'linear')
      clearInterval(steps)
    }
  }

  const go = async (stop: Stop) => {
    if (walking) return
    unlockSound()
    if (stop.state === 'Locked') {
      playSound('wrong')
      shake(`[data-stop="${stop.id}"]`)
      setNotice(stop.boss ? t('kids.map.bossLocked') : t('kids.map.stopLocked'))
      return
    }
    setNotice(null)
    playSound('tap')
    setWalking(stop)
    await walkTo(stop.at)
    playSound(stop.boss ? 'hit' : 'whoosh')
    await pause(0.3, still)
    onOpenLesson(stop.id)
    void navigate(`/kids/lessons/${stop.id}`)
  }

  const leave = async () => {
    if (walking) return
    unlockSound()
    playSound('tap')
    setWalking('boat')
    await walkTo(0)
    playSound('splash')
    onLeave()
  }

  const lessonStops = stops.filter((stop) => !stop.boss)
  const bossStop = stops.find((stop) => stop.boss)
  // The stop to go to next glows.
  const next = stops.find((stop) => stop.state === 'Available')?.id

  return (
    <motion.section
      aria-labelledby="island-title"
      initial={{ opacity: 0, scale: 1.25 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: still ? 0 : 0.6, ease: 'easeOut' }}
      className="size-full"
      ref={scope}
    >
      <Stage
        horizon={0}
        sky={['#4fb7e3', '#4fb7e3']}
        ground={['#4fb7e3', '#2b86cf']}
        hud={
          <>
            <div className="pointer-events-none absolute inset-x-0 top-0 flex flex-col items-start gap-2 p-3 pr-44 sm:flex-row sm:items-center sm:gap-4 sm:p-4 sm:pr-56">
              {greeting}
              <h1
                id="island-title"
                className="rounded-2xl border-4 border-[#7a4b22] bg-[#c8924f] px-4 py-1 font-heading text-lg leading-tight font-bold text-white shadow-md sm:text-2xl lg:text-3xl"
              >
                <span className="block text-sm font-semibold opacity-90 sm:text-base">
                  {levelLabel(t, index + 1, level.title)}
                </span>
                {level.description ?? level.title}
              </h1>
            </div>
            <div className="absolute inset-x-0 bottom-0 flex flex-wrap items-end justify-between gap-3 p-3 sm:p-5">
              <p
                aria-live="polite"
                className="max-w-[60%] rounded-full bg-white/90 px-5 py-2 text-base font-bold text-foreground shadow-md sm:text-xl lg:text-2xl"
              >
                {walking === 'boat'
                  ? t('kids.map.toBoat')
                  : walking
                    ? t('kids.map.walking', { place: walking.title })
                    : (notice ?? t('kids.map.pickStop'))}
              </p>
              <Button
                variant="secondary"
                className="h-auto min-h-12 rounded-full px-5 text-base shadow-md sm:min-h-14 sm:text-xl"
                disabled={walking !== null}
                onClick={() => void leave()}
              >
                <Sailboat aria-hidden className="size-6" />
                {t('kids.map.toSea')}
              </Button>
            </div>
          </>
        }
      >
        <svg viewBox={`0 0 ${VIEW} ${HEIGHT}`} className="absolute inset-0 size-full overflow-visible" aria-hidden>
          <defs>
            <radialGradient id="island-sea" cx="0.5" cy="0.5" r="0.75">
              <stop offset="0.5" stopColor="#6fd3e6" />
              <stop offset="1" stopColor="#4fb7e3" />
            </radialGradient>
          </defs>
          {/* The sea goes on past the stage on wide or tall screens. */}
          <rect x={-VIEW * 2} y={-HEIGHT * 2} width={VIEW * 5} height={HEIGHT * 5} fill="#4fb7e3" />
          <rect width={VIEW} height={HEIGHT} fill="url(#island-sea)" />
          <WaveRows from={-VIEW * 1.5} to={VIEW * 2.5} top={-HEIGHT} bottom={HEIGHT * 2} />
          <path
            d="M40 560 C0 420 110 250 250 190 C370 80 640 50 820 100 C970 150 1000 330 950 450 C905 570 700 610 500 595 C300 610 110 610 40 560 Z"
            fill="#a7efe9"
            opacity="0.7"
            className="kids-shimmer"
          />
          <path
            d="M70 545 C30 420 130 270 260 210 C380 105 640 75 815 122 C950 170 975 330 930 445 C890 550 700 585 500 575 C300 588 130 588 70 545 Z"
            fill={theme.sand}
          />
          <path
            d="M115 515 C85 410 170 290 285 238 C395 140 640 112 800 152 C915 192 935 330 895 430 C860 520 690 548 500 540 C320 552 160 552 115 515 Z"
            fill={theme.grass}
          />
          {/* The boss's mountain and its cave. */}
          <path d="M700 205 L830 40 L960 215 Z" fill={theme.hill} />
          <path d="M830 40 L862 82 Q838 92 812 80 Z" fill="#ffffff" opacity="0.7" />
          <path d="M790 215 Q790 150 830 150 Q870 150 870 215 Z" fill="#3b2a20" />
          {trees.map((tree) => (
            <Decor key={`${tree.x}-${tree.y}`} kind={theme.decor} x={tree.x} y={tree.y} size={tree.size} />
          ))}
          {/* The footpath, sandy with white stones down its middle. */}
          <path d={svgPath(footpath)} stroke={theme.sand} strokeWidth="34" fill="none" strokeLinecap="round" />
          <path
            d={svgPath(footpath)}
            stroke="#ffffff"
            strokeWidth="5"
            strokeDasharray="4 22"
            fill="none"
            strokeLinecap="round"
            opacity="0.8"
          />
        </svg>

        {/* The boat waiting at the beach. */}
        <div
          className="kids-bob absolute w-[13cqw] -translate-x-1/2 -translate-y-1/2"
          style={{ left: left(95), top: top(555) }}
        >
          <div className="aspect-[8/5]">
            <Boat rowing={false} />
          </div>
        </div>

        <ol aria-label={t('kids.map.lessonsOf', { level: level.title })}>
          {lessonStops.map((stop) => {
            const point = trail.at(stop.at)
            return (
              <li
                key={stop.id}
                className="absolute -translate-x-1/2 -translate-y-[85%]"
                style={{ left: left(point.x), top: top(point.y) }}
              >
                <Link
                  to={`/kids/lessons/${stop.id}`}
                  onClick={(event) => {
                    event.preventDefault()
                    void go(stop)
                  }}
                  aria-label={t('kids.map.lesson', { number: stop.number, title: stop.title })}
                  aria-disabled={stop.state === 'Locked' ? true : undefined}
                  aria-describedby={`stop-${stop.id}-state`}
                  data-stop={stop.id}
                  className="group relative flex flex-col items-center focus-visible:outline-none"
                >
                  {stop.id === next && (
                    <span
                      aria-hidden
                      className="kids-pulse absolute top-0 size-[6.5cqw] rounded-full bg-amber-300/70"
                    />
                  )}
                  <span
                    className={cn(
                      'relative flex size-[6.5cqw] items-center justify-center rounded-full border-[0.4cqw] border-white font-heading text-[3cqw] font-bold text-white shadow-lg transition-transform group-hover:scale-110 group-focus-visible:ring-[0.6cqw] group-focus-visible:ring-white',
                      stop.state === 'Completed'
                        ? 'bg-green-500'
                        : stop.state === 'Locked'
                          ? 'bg-slate-400'
                          : 'bg-[#f59e0b]',
                    )}
                  >
                    {stop.state === 'Locked' ? <Lock aria-hidden className="size-[2.6cqw]" /> : stop.number}
                  </span>
                  <StopStars stop={stop} />
                  <span className="h-[1.6cqw] w-[0.8cqw] bg-[#7a4b22]" />
                  <span className="max-w-[16cqw] truncate rounded-full bg-white/90 px-[1.2cqw] py-[0.2cqw] text-[1.7cqw] font-bold text-foreground shadow">
                    {stop.title}
                  </span>
                </Link>
              </li>
            )
          })}
        </ol>

        {bossStop && (
          <Link
            to={`/kids/lessons/${bossStop.id}`}
            onClick={(event) => {
              event.preventDefault()
              void go(bossStop)
            }}
            aria-label={t('kids.map.bossStop', { name: bossStop.title })}
            aria-disabled={bossStop.state === 'Locked' ? true : undefined}
            aria-describedby={`stop-${bossStop.id}-state`}
            data-stop={bossStop.id}
            className="group absolute flex -translate-x-1/2 -translate-y-full flex-col items-center focus-visible:outline-none"
            style={{ left: left(830), top: top(150) }}
          >
            {bossStop.state === 'Available' && (
              <span className="kids-pulse absolute top-[2cqw] size-[11cqw] rounded-full bg-red-500/40" />
            )}
            <span
              className={cn(
                'relative overflow-hidden rounded-full border-[0.5cqw] bg-white shadow-xl transition-transform group-hover:scale-110 group-focus-visible:ring-[0.6cqw] group-focus-visible:ring-white',
                bossStop.state === 'Locked' ? 'border-slate-400 grayscale' : 'border-red-500',
              )}
            >
              <KidsPicture url={bossStop.imageUrl} alt="" className="size-[11cqw]" />
              {bossStop.state === 'Locked' && (
                <span className="absolute inset-0 flex items-center justify-center bg-slate-900/40">
                  <Lock aria-hidden className="size-[4cqw] text-white" />
                </span>
              )}
            </span>
            <StopStars stop={bossStop} />
            <span className="relative -mt-[1.4cqw] flex items-center gap-[0.5cqw] rounded-full bg-red-500 px-[1.2cqw] py-[0.2cqw] text-[1.7cqw] font-bold text-white shadow">
              <Swords aria-hidden className="size-[1.8cqw]" />
              {bossStop.title}
            </span>
          </Link>
        )}

        <motion.div
          className="pointer-events-none absolute -translate-x-1/2 -translate-y-full"
          style={{ left: heroLeft, top: heroTop }}
        >
          <div style={{ transform: `scaleX(${facing})` }}>
            <Explorer avatarKey={avatarKey} walking={walking !== null} className="size-[7cqw]" />
          </div>
        </motion.div>
      </Stage>
    </motion.section>
  )
}

/** A stop's best stars once completed, and its state in words for screen readers. */
function StopStars({ stop }: { stop: Stop }) {
  const { t } = useTranslation()
  return (
    <span className="relative -mt-[0.8cqw] flex">
      <span id={`stop-${stop.id}-state`} className="sr-only">
        {stop.state === 'Completed'
          ? t('kids.map.stopCompleted', { count: stop.stars })
          : stop.state === 'Locked'
            ? t('kids.map.stopLockedState')
            : t('kids.map.stopAvailable')}
      </span>
      {stop.state === 'Completed' &&
        [1, 2, 3].map((position) => (
          <Star
            key={position}
            aria-hidden
            className={cn(
              'size-[2cqw] drop-shadow',
              position <= stop.stars ? 'fill-yellow-400 text-yellow-500' : 'fill-white/70 text-white/70',
            )}
          />
        ))}
    </span>
  )
}
