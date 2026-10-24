import { motion, motionValue, useMotionValue, useReducedMotion, useTransform, type MotionValue } from 'motion/react'
import { useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Boat, Cloud, Explorer, IslandArt, Mainland, WaveRows } from '@/areas/kids/adventure/scenery'
import { playSound, unlockSound } from '@/areas/kids/adventure/sound'
import { crossing, glide, svgPath, track, type Point } from '@/areas/kids/adventure/track'
import { type MapLevel } from '@/areas/kids/play/api'
import { cn } from '@/shared/lib/utils'

// The screen shows 1000 × 600 units of the world; the world grows to the right with each level's island.
const VIEW = 1000
const HEIGHT = 600
const HORIZON = 150
const MAINLAND_STAND: Point = { x: 250, y: 470 }
const MAINLAND_DOCK: Point = { x: 395, y: 500 }

const islandCenter = (index: number): Point => ({ x: 690 + index * 430, y: index % 2 ? 260 : 350 })
const worldWidth = (islands: number) => Math.max(VIEW, islandCenter(islands - 1).x + 320)

/** Where the character stands and where its boat waits: -1 is the mainland, otherwise a level's index. */
function standOf(at: number): Point {
  if (at < 0) return MAINLAND_STAND
  const center = islandCenter(at)
  return { x: center.x - 110, y: center.y + 60 }
}
function dockOf(at: number): Point {
  if (at < 0) return MAINLAND_DOCK
  const center = islandCenter(at)
  return { x: center.x - 175, y: center.y + 105 }
}

/** World units as a share of the world's width or height, for placing HTML on the drawing. */
const across = (x: number, width: number) => `${(x / width) * 100}%`
const down = (y: number) => `${(y / HEIGHT) * 100}%`

interface SeaSceneProps {
  levels: MapLevel[]
  /** Where the child is: -1 on the mainland, otherwise the index of the level's island. */
  at: number
  avatarKey: string
  greeting: ReactNode
  onArrive: (level: MapLevel) => void
}

/**
 * The sea between the mainland and the levels' islands (owner 2026-10-24). The child taps an island: the character
 * walks to its boat, rows across (the camera follows, the sea splashes), steps onto the island and the view dives into
 * it. Layers move at different speeds as the camera pans, for depth. Every island can be visited for now; reaching the
 * next one only by beating the boss comes with S4.2.
 */
export function SeaScene({ levels, at, avatarKey, greeting, onArrive }: SeaSceneProps) {
  const { t } = useTranslation()
  const still = useReducedMotion() ?? false
  const width = worldWidth(levels.length)
  const here = useRef(at)

  const heroX = useMotionValue(standOf(at).x)
  const heroY = useMotionValue(standOf(at).y)
  const boatX = useMotionValue(dockOf(at).x)
  const boatY = useMotionValue(dockOf(at).y)
  const heroLeft = useTransform(heroX, (x) => across(x, width))
  const heroTop = useTransform(heroY, down)
  const boatLeft = useTransform(boatX, (x) => across(x, width))
  const boatTop = useTransform(boatY, down)
  const camera = useTransform(boatX, (x) => Math.min(Math.max(x - VIEW / 2, 0), width - VIEW))
  const zoom = useMotionValue(1)
  const fade = useTransform(zoom, [1, 2.6], [1, 0])

  const [pose, setPose] = useState<'idle' | 'walk' | 'row'>('idle')
  const [aboard, setAboard] = useState(false)
  const [facing, setFacing] = useState(1)
  const [trip, setTrip] = useState<MapLevel | null>(null)
  const [origin, setOrigin] = useState('50% 50%')

  const walk = async (to: Point) => {
    const distance = Math.hypot(to.x - heroX.get(), to.y - heroY.get())
    if (distance < 1) return
    setFacing(to.x >= heroX.get() ? 1 : -1)
    setPose('walk')
    const steps = still ? undefined : setInterval(() => playSound('step'), 300)
    const seconds = Math.max(0.4, distance / 170)
    await Promise.all([glide(heroX, to.x, seconds, still, 'linear'), glide(heroY, to.y, seconds, still, 'linear')])
    clearInterval(steps)
    setPose('idle')
  }

  const row = async (from: number, to: number) => {
    await walk(dockOf(from))
    setAboard(true)
    playSound('splash')
    const route = track(crossing(dockOf(from), dockOf(to)))
    const progress = motionValue(0)
    const follow = progress.on('change', (fraction) => {
      const point = route.at(fraction)
      boatX.set(point.x)
      boatY.set(point.y)
    })
    setFacing(dockOf(to).x >= dockOf(from).x ? 1 : -1)
    setPose('row')
    const splashes = still ? undefined : setInterval(() => playSound('splash'), 900)
    await glide(progress, 1, Math.min(6, 1.4 + route.length / 380), still)
    clearInterval(splashes)
    follow()
    heroX.set(dockOf(to).x)
    heroY.set(dockOf(to).y)
    setAboard(false)
    setPose('idle')
  }

  const visit = async (index: number) => {
    if (trip) return
    unlockSound()
    playSound('tap')
    const level = levels[index]!
    setTrip(level)
    if (index !== here.current) await row(here.current, index)
    here.current = index
    await walk(standOf(index))
    // Dive into the island.
    const center = islandCenter(index)
    setOrigin(`${((center.x - camera.get()) / VIEW) * 100}% ${(center.y / HEIGHT) * 100}%`)
    playSound('whoosh')
    await glide(zoom, 2.6, 0.8, still, 'easeIn')
    onArrive(level)
  }

  // The child only leaves `at` on a trip, which ends in the island.
  const next = trip ? -1 : at + 1

  return (
    <div className="@container relative aspect-[5/3] w-full overflow-hidden rounded-[2rem] bg-[#8fd8ff] shadow-xl select-none">
      <motion.div className="absolute inset-0" style={{ scale: zoom, opacity: fade, transformOrigin: origin }}>
        {/* The sky and the sun stay; the clouds, the far islets and the world move slower or faster than each other. */}
        <svg viewBox={`0 0 ${VIEW} ${HEIGHT}`} className="absolute inset-0 size-full" aria-hidden>
          <defs>
            <linearGradient id="sea-sky" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#6cc8ff" />
              <stop offset="1" stopColor="#dff5ff" />
            </linearGradient>
          </defs>
          <rect width={VIEW} height={HORIZON + 10} fill="url(#sea-sky)" />
          <g transform="translate(860 70)">
            <g className="kids-spin">
              {Array.from({ length: 12 }, (_, index) => (
                <rect
                  key={index}
                  x="-3"
                  y="-62"
                  width="6"
                  height="18"
                  rx="3"
                  fill="#ffd34d"
                  transform={`rotate(${index * 30})`}
                />
              ))}
            </g>
            <circle r="36" fill="#ffd34d" />
            <circle r="28" fill="#ffe27a" />
          </g>
        </svg>

        <Layer
          camera={camera}
          world={width}
          depth={0.15}
          drawing={(layer) =>
            Array.from({ length: Math.ceil(layer / 260) }, (_, index) => (
              <Cloud
                key={index}
                x={120 + index * 260}
                y={40 + (index % 3) * 26}
                size={0.7 + (index % 2) * 0.4}
                slow={index % 2 === 0}
              />
            ))
          }
        />

        <Layer
          camera={camera}
          world={width}
          depth={0.4}
          drawing={(layer) => (
            <>
              <rect y={HORIZON} width={layer} height={HEIGHT - HORIZON} fill="#7fd6e8" />
              {Array.from({ length: Math.ceil(layer / 330) }, (_, index) => (
                <path
                  key={index}
                  d={`M${60 + index * 330} ${HORIZON + 2} q40 -${22 + (index % 3) * 8} 90 0 Z`}
                  fill="#5fb3c9"
                  opacity="0.7"
                />
              ))}
            </>
          )}
        />

        <Layer
          camera={camera}
          world={width}
          depth={1}
          drawing={(layer) => (
            <>
              <defs>
                <linearGradient id="sea-water" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0" stopColor="#6fd3e6" />
                  <stop offset="1" stopColor="#2b86cf" />
                </linearGradient>
              </defs>
              <rect y={HORIZON + 14} width={layer} height={HEIGHT} fill="url(#sea-water)" />
              <WaveRows width={layer} from={HORIZON + 40} to={HEIGHT - 20} />
              <Mainland />
              {/* The sea lanes between the docks, dotted like a treasure map. */}
              <g
                stroke="#ffffff"
                strokeWidth="4"
                strokeDasharray="2 14"
                strokeLinecap="round"
                fill="none"
                opacity="0.7"
              >
                {levels.map((level, index) => (
                  <path key={level.id} d={svgPath(crossing(dockOf(index - 1), dockOf(index)))} />
                ))}
              </g>
            </>
          )}
        >
          <ol aria-label={t('kids.map.label')}>
            {levels.map((level, index) => (
              <IslandButton
                key={level.id}
                level={level}
                index={index}
                width={width}
                next={index === next}
                disabled={trip !== null}
                onVisit={() => void visit(index)}
              />
            ))}
          </ol>

          <motion.div
            className="absolute w-[14cqw] -translate-x-1/2 -translate-y-[62%]"
            style={{ left: boatLeft, top: boatTop }}
          >
            <div className={cn('relative aspect-[8/5]', pose === 'row' ? 'kids-rock' : 'kids-bob')}>
              <div style={{ transform: `scaleX(${facing})` }} className="size-full">
                <Boat rowing={pose === 'row'} />
              </div>
              {aboard && (
                <Explorer
                  avatarKey={avatarKey}
                  walking={false}
                  className="absolute top-[-38%] left-[34%] size-[6cqw]"
                />
              )}
            </div>
          </motion.div>

          {!aboard && (
            <motion.div
              className="absolute -translate-x-1/2 -translate-y-full"
              style={{ left: heroLeft, top: heroTop }}
            >
              <div style={{ transform: `scaleX(${facing})` }}>
                <Explorer avatarKey={avatarKey} walking={pose === 'walk'} className="size-[7cqw]" />
              </div>
            </motion.div>
          )}
        </Layer>

        <Layer
          camera={camera}
          world={width}
          depth={1.25}
          className="pointer-events-none"
          drawing={(layer) => <WaveRows width={layer} from={HEIGHT - 12} to={HEIGHT - 12} />}
        />
      </motion.div>

      {/* The child's corner and what is happening, read out to screen readers. */}
      <div className="pointer-events-none absolute top-[3%] left-[3%]">{greeting}</div>
      <p
        aria-live="polite"
        className="absolute bottom-[4%] left-1/2 max-w-[80%] -translate-x-1/2 rounded-full bg-white/90 px-[2.4cqw] py-[1cqw] text-center text-[2.2cqw] font-bold text-foreground shadow-md"
      >
        {trip ? t('kids.map.sailing', { level: trip.title }) : t('kids.map.pickIsland')}
      </p>
    </div>
  )
}

/**
 * A layer of the world, moved by the camera at its own depth: 1 moves with the world, less is farther away, more is
 * nearer. `drawing` draws in SVG; children are HTML placed on top, in shares of the layer's size.
 */
function Layer({
  camera,
  world,
  depth,
  className,
  drawing,
  children,
}: {
  camera: MotionValue<number>
  world: number
  depth: number
  className?: string
  drawing: (width: number) => ReactNode
  children?: ReactNode
}) {
  const width = VIEW + (world - VIEW) * depth
  const x = useTransform(camera, (left) => `${(-(left * depth) / width) * 100}%`)

  return (
    <motion.div className={cn('absolute inset-y-0 left-0', className)} style={{ width: `${(width / VIEW) * 100}%`, x }}>
      <svg viewBox={`0 0 ${width} ${HEIGHT}`} className="absolute inset-0 size-full" aria-hidden>
        {drawing(width)}
      </svg>
      {children}
    </motion.div>
  )
}

/** A level's island, floating gently, with its name on a wooden sign. The next island to visit has a bouncing hand. */
function IslandButton({
  level,
  index,
  width,
  next,
  disabled,
  onVisit,
}: {
  level: MapLevel
  index: number
  width: number
  next: boolean
  disabled: boolean
  onVisit: () => void
}) {
  const { t } = useTranslation()
  const center = islandCenter(index)

  return (
    <li
      className="absolute"
      style={{
        left: across(center.x - 200, width),
        top: down(center.y - 175),
        width: across(400, width),
        height: down(300),
      }}
    >
      <motion.button
        type="button"
        disabled={disabled}
        onClick={onVisit}
        aria-label={t('kids.map.island', { number: index + 1, title: level.title })}
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.97 }}
        className="relative flex size-full flex-col items-center rounded-[3cqw] focus-visible:ring-[0.6cqw] focus-visible:ring-white focus-visible:outline-none disabled:cursor-default"
      >
        <span className="relative z-10 flex flex-col items-center rounded-[1.4cqw] border-[0.3cqw] border-[#7a4b22] bg-[#c8924f] px-[1.6cqw] py-[0.5cqw] text-white shadow-md">
          <span className="text-[1.5cqw] leading-tight font-semibold opacity-90">
            {t('kids.map.level', { number: index + 1 })}
          </span>
          <span className="max-w-[24cqw] truncate font-heading text-[2.2cqw] leading-tight font-bold">
            {level.title}
          </span>
        </span>
        {next && (
          <span aria-hidden className="kids-hint absolute -top-[16%] text-[3.2cqw]">
            👇
          </span>
        )}
        <span className="kids-bob block w-full flex-1">
          <IslandArt index={index} boss={level.boss !== null} />
        </span>
      </motion.button>
    </li>
  )
}
