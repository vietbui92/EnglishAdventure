import { motion, useMotionValue, useReducedMotion, useTransform } from 'motion/react'
import { Sailboat, Swords } from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Boat, Decor, Explorer, themeOf, WaveRows } from '@/areas/kids/adventure/scenery'
import { playSound, unlockSound } from '@/areas/kids/adventure/sound'
import { glide, pause, smooth, svgPath, track, type Point } from '@/areas/kids/adventure/track'
import { type MapLevel } from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { Button } from '@/shared/components/ui/button'

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
}

function stopsOf(level: MapLevel): Stop[] {
  const lessons = level.lessons.map((lesson, index) => ({
    id: lesson.id,
    at: ((index + 1) / (level.lessons.length + 1)) * 0.88,
    boss: false,
    title: lesson.title,
    number: index + 1,
    imageUrl: null,
  }))
  const boss = level.boss
    ? [{ id: level.boss.id, at: 1, boss: true, title: level.boss.name, number: 0, imageUrl: level.boss.imageUrl }]
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
 * walks back down and sails off to the sea.
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

  return (
    <motion.section
      aria-labelledby="island-title"
      initial={{ opacity: 0, scale: 1.25 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: still ? 0 : 0.6, ease: 'easeOut' }}
      className="@container relative aspect-[5/3] w-full overflow-hidden rounded-[2rem] bg-[#4fb7e3] shadow-xl select-none"
    >
      <svg viewBox={`0 0 ${VIEW} ${HEIGHT}`} className="absolute inset-0 size-full" aria-hidden>
        <defs>
          <radialGradient id="island-sea" cx="0.5" cy="0.5" r="0.75">
            <stop offset="0.5" stopColor="#6fd3e6" />
            <stop offset="1" stopColor="#2b86cf" />
          </radialGradient>
        </defs>
        <rect width={VIEW} height={HEIGHT} fill="url(#island-sea)" />
        <WaveRows width={VIEW} from={30} to={HEIGHT - 10} />
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

      <h1
        id="island-title"
        className="absolute top-[3%] left-1/2 -translate-x-1/2 rounded-[1.4cqw] border-[0.3cqw] border-[#7a4b22] bg-[#c8924f] px-[2cqw] py-[0.6cqw] text-center font-heading text-[2.6cqw] leading-tight font-bold text-white shadow-md"
      >
        <span className="block text-[1.6cqw] font-semibold opacity-90">
          {t('kids.map.level', { number: index + 1 })}
        </span>
        {level.title}
      </h1>

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
                className="group flex flex-col items-center focus-visible:outline-none"
              >
                <span className="flex size-[6.5cqw] items-center justify-center rounded-full border-[0.4cqw] border-white bg-[#f59e0b] font-heading text-[3cqw] font-bold text-white shadow-lg transition-transform group-hover:scale-110 group-focus-visible:ring-[0.6cqw] group-focus-visible:ring-white">
                  {stop.number}
                </span>
                <span className="h-[2.4cqw] w-[0.8cqw] bg-[#7a4b22]" />
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
          className="group absolute flex -translate-x-1/2 -translate-y-full flex-col items-center focus-visible:outline-none"
          style={{ left: left(830), top: top(150) }}
        >
          <span className="kids-pulse absolute top-[2cqw] size-[11cqw] rounded-full bg-red-500/40" />
          <span className="relative overflow-hidden rounded-full border-[0.5cqw] border-red-500 bg-white shadow-xl transition-transform group-hover:scale-110 group-focus-visible:ring-[0.6cqw] group-focus-visible:ring-white">
            <KidsPicture url={bossStop.imageUrl} alt="" className="size-[11cqw]" />
          </span>
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

      <div className="pointer-events-none absolute top-[3%] left-[3%]">{greeting}</div>
      <Button
        variant="secondary"
        className="absolute right-[3%] bottom-[4%] h-auto min-h-12 rounded-full px-[2cqw] text-[2cqw] shadow-md"
        disabled={walking !== null}
        onClick={() => void leave()}
      >
        <Sailboat aria-hidden className="size-[2.4cqw]" />
        {t('kids.map.toSea')}
      </Button>
      <p
        aria-live="polite"
        className="absolute bottom-[4%] left-[3%] max-w-[55%] rounded-full bg-white/90 px-[2cqw] py-[0.8cqw] text-[2cqw] font-bold text-foreground shadow-md"
      >
        {walking === 'boat'
          ? t('kids.map.toBoat')
          : walking
            ? t('kids.map.walking', { place: walking.title })
            : t('kids.map.pickStop')}
      </p>
    </motion.section>
  )
}
