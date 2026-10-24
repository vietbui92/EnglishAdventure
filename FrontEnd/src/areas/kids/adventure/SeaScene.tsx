import { motion, motionValue, useMotionValue, useReducedMotion, useTransform } from 'motion/react'
import { useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { across, down, follow, HEIGHT, Layer, spread, VIEW } from '@/areas/kids/adventure/Layer'
import { useNudges } from '@/areas/kids/adventure/effects'
import { levelLabel } from '@/areas/kids/adventure/labels'
import { Boat, Cloud, Explorer, IslandArt, Mainland, WaveRows } from '@/areas/kids/adventure/scenery'
import { FishShadows, FoamWaves, Gulls, LeapingFish, SeaMist } from '@/areas/kids/adventure/sea'
import { playSound, unlockSound } from '@/areas/kids/adventure/sound'
import { Stage } from '@/areas/kids/adventure/Stage'
import { crossing, glide, svgPath, track, type Point } from '@/areas/kids/adventure/track'
import { type MapLevel } from '@/areas/kids/play/api'
import { cn } from '@/shared/lib/utils'

// An overview of the sea (owner 2026-10-24: see many islands at once): the mainland is drawn at MAINLAND_SCALE in
// the bottom left corner, and the islands are scattered on three rows, zigzagging away to the right.
const HORIZON = 110
const MAINLAND_SCALE = 0.75
const onMainland = (point: Point): Point => ({
  x: point.x * MAINLAND_SCALE,
  y: HEIGHT - (HEIGHT - point.y) * MAINLAND_SCALE,
})
const MAINLAND_STAND = onMainland({ x: 250, y: 470 })
const MAINLAND_DOCK = onMainland({ x: 395, y: 500 })

// An island is ISLAND_WIDTH across: its sign above, its drawing (400 × 260, scaled) below.
const ISLAND_WIDTH = 220
const SIGN_HEIGHT = 56
const ART_HEIGHT = (ISLAND_WIDTH * 260) / 400
const rows = [235, 425, 300, 470, 250, 405]
const islandCenter = (index: number): Point => ({ x: 410 + index * 235, y: rows[index % rows.length]! })
const worldWidth = (islands: number) => Math.max(VIEW, islandCenter(islands - 1).x + 220)

/** Where the character stands and where its boat waits: -1 is the mainland, otherwise a level's index. */
function standOf(at: number): Point {
  if (at < 0) return MAINLAND_STAND
  const center = islandCenter(at)
  return { x: center.x - 50, y: center.y + 30 }
}
function dockOf(at: number): Point {
  if (at < 0) return MAINLAND_DOCK
  const center = islandCenter(at)
  return { x: center.x - 100, y: center.y + 42 }
}

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
  // Where the camera looks: the boat while sailing, anywhere the child drags the map otherwise.
  const look = useMotionValue(boatX.get())
  const camera = useTransform(look, (x) => follow(x, width))
  const pannedAt = useRef(-Infinity)
  const zoom = useMotionValue(1)
  const fade = useTransform(zoom, [1, 2.6], [1, 0])

  const [pose, setPose] = useState<'idle' | 'walk' | 'row'>('idle')
  const [aboard, setAboard] = useState(false)
  const [facing, setFacing] = useState(1)
  const [trip, setTrip] = useState<MapLevel | null>(null)
  const [origin, setOrigin] = useState('50% 50%')
  const [notice, setNotice] = useState<string | null>(null)
  // The stage: shaken islands are found in it, and its width turns a drag into world units.
  const { scope, shake } = useNudges()

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
    const unfollow = progress.on('change', (fraction) => {
      const point = route.at(fraction)
      boatX.set(point.x)
      boatY.set(point.y)
      look.set(point.x)
    })
    setFacing(dockOf(to).x >= dockOf(from).x ? 1 : -1)
    setPose('row')
    const splashes = still ? undefined : setInterval(() => playSound('splash'), 900)
    await glide(progress, 1, Math.min(6, 1.4 + route.length / 380), still)
    clearInterval(splashes)
    unfollow()
    heroX.set(dockOf(to).x)
    heroY.set(dockOf(to).y)
    setAboard(false)
    setPose('idle')
  }

  const visit = async (index: number, tappedAt: number) => {
    // A drag that ends on an island only looks around.
    if (trip || tappedAt - pannedAt.current < 250) return
    unlockSound()
    const level = levels[index]!
    if (!level.unlocked) {
      // Still in the mist: the boss before it opens the way (S4.2).
      playSound('wrong')
      shake(`[data-island="${level.id}"]`)
      setNotice(t('kids.map.islandLocked', { boss: levels[index - 1]?.boss?.name ?? '' }))
      return
    }
    setNotice(null)
    playSound('tap')
    setTrip(level)
    // Back to the boat, if the child had looked elsewhere.
    await glide(look, boatX.get(), Math.min(1, Math.abs(look.get() - boatX.get()) / 1500), still)
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

  // The island to go to next: the first one open whose boss is not defeated yet.
  const next = trip ? -1 : levels.findIndex((level) => level.unlocked && !level.beaten)

  return (
    <Stage
      minWidth={800}
      horizon={HORIZON + 14}
      sky={['#5f8fb5', '#d4e9f1']}
      ground={['#4ba9c6', '#1b5a8c']}
      hud={
        <>
          {/* The child's corner and what is happening, read out to screen readers. */}
          {/* Mist darkens the far corners a little. */}
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_center,transparent_55%,rgba(20,40,70,0.35))]" />
          <div className="pointer-events-none absolute top-3 left-3 sm:top-4 sm:left-4">{greeting}</div>
          <p
            aria-live="polite"
            className="absolute bottom-4 left-1/2 w-max max-w-[90%] -translate-x-1/2 rounded-full bg-white/90 px-5 py-2 text-center text-base font-bold text-foreground shadow-md sm:bottom-6 sm:text-xl lg:text-2xl"
          >
            {trip ? t('kids.map.sailing', { level: trip.title }) : (notice ?? t('kids.map.pickIsland'))}
            {!trip && width > VIEW && (
              <span className="block text-sm font-semibold text-muted-foreground sm:text-base">
                {t('kids.map.dragHint')}
              </span>
            )}
          </p>
        </>
      }
    >
      <motion.div
        ref={scope}
        className="absolute inset-0 cursor-grab touch-none active:cursor-grabbing"
        style={{ scale: zoom, opacity: fade, transformOrigin: origin }}
        onPan={(_, info) => {
          if (trip || !scope.current) return
          // Screen pixels to world units: the stage is VIEW units across.
          const units = (info.delta.x * VIEW) / scope.current.offsetWidth
          look.set(Math.min(Math.max(look.get() - units, VIEW / 2), width - VIEW / 2))
        }}
        onPanEnd={(event) => {
          pannedAt.current = event.timeStamp
        }}
      >
        {/* The sun stays; the clouds, the far islets and the world move slower or faster than each other. */}
        <Layer
          camera={camera}
          world={width}
          depth={0.05}
          drawing={(from, to) => (
            <>
              <defs>
                <linearGradient id="sea-sky" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0" stopColor="#5f8fb5" />
                  <stop offset="1" stopColor="#d4e9f1" />
                </linearGradient>
              </defs>
              <rect x={from} width={to - from} height={HORIZON + 10} fill="url(#sea-sky)" />
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
            </>
          )}
        />

        <Layer
          camera={camera}
          world={width}
          depth={0.15}
          drawing={(from, to) =>
            spread(from, to, 260).map((x, index) => (
              <Cloud
                key={x}
                x={x + 120}
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
          depth={0.25}
          drawing={(from, to) => <Gulls from={from} to={to} top={20} />}
        />

        <Layer
          camera={camera}
          world={width}
          depth={0.4}
          drawing={(from, to) => (
            <>
              <rect x={from} y={HORIZON} width={to - from} height={HEIGHT - HORIZON} fill="#6db8cf" />
              {spread(from, to, 330).map((x, index) => (
                <path
                  key={x}
                  d={`M${x + 60} ${HORIZON + 2} q40 -${22 + (Math.abs(index) % 3) * 8} 90 0 Z`}
                  fill="#4b8ea6"
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
          drawing={(from, to) => (
            <>
              <defs>
                <linearGradient id="sea-water" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0" stopColor="#4ba9c6" />
                  <stop offset="1" stopColor="#1b5a8c" />
                </linearGradient>
              </defs>
              <rect x={from} y={HORIZON + 14} width={to - from} height={HEIGHT} fill="url(#sea-water)" />
              <SeaMist from={from} to={to} y={HORIZON + 12} opacity={0.55} />
              <WaveRows from={from} to={to} top={HORIZON + 40} bottom={HEIGHT - 20} />
              <FishShadows from={from} to={to} top={HORIZON + 80} bottom={HEIGHT - 40} />
              <LeapingFish from={from} to={to} top={HORIZON + 90} bottom={HEIGHT - 60} />
              <g transform={`translate(0 ${HEIGHT * (1 - MAINLAND_SCALE)}) scale(${MAINLAND_SCALE})`}>
                <Mainland />
              </g>
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
                onVisit={(tappedAt) => void visit(index, tappedAt)}
              />
            ))}
          </ol>

          <motion.div
            className="absolute w-[9cqw] -translate-x-1/2 -translate-y-[62%]"
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
                  className="absolute top-[-40%] left-[33%] size-[4cqw]"
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
                <Explorer avatarKey={avatarKey} walking={pose === 'walk'} className="size-[5cqw]" />
              </div>
            </motion.div>
          )}
        </Layer>

        <Layer
          camera={camera}
          world={width}
          depth={1.25}
          className="pointer-events-none"
          drawing={(from, to) => (
            <>
              <SeaMist from={from} to={to} y={300} opacity={0.12} />
              <FoamWaves from={from} to={to} y={HEIGHT - 18} />
            </>
          )}
        />
      </motion.div>
    </Stage>
  )
}

/**
 * A level's island, floating gently, with a wooden sign: "Level n · title", and the island's name below, which is the
 * level's description (owner 2026-10-24). The next island to visit has a bouncing hand.
 */
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
  onVisit: (tappedAt: number) => void
}) {
  const { t } = useTranslation()
  const center = islandCenter(index)

  return (
    <li
      className="absolute"
      style={{
        left: across(center.x - ISLAND_WIDTH / 2, width),
        top: down(center.y - ART_HEIGHT / 2 - SIGN_HEIGHT - 10),
        width: across(ISLAND_WIDTH, width),
        height: down(SIGN_HEIGHT + ART_HEIGHT + 10),
      }}
    >
      <motion.button
        type="button"
        disabled={disabled}
        onClick={(event) => onVisit(event.timeStamp)}
        aria-label={t('kids.map.island', { number: index + 1, title: level.title })}
        aria-disabled={level.unlocked ? undefined : true}
        aria-describedby={`island-${level.id}-state`}
        data-island={level.id}
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.97 }}
        className="group relative flex size-full flex-col items-center rounded-[3cqw] focus-visible:ring-[0.6cqw] focus-visible:ring-white focus-visible:outline-none disabled:cursor-default"
      >
        <span className="relative z-10 flex max-w-[26cqw] flex-col items-center rounded-[1.2cqw] border-[0.3cqw] border-[#7a4b22] bg-[#c8924f] px-[1.4cqw] py-[0.4cqw] text-white shadow-md">
          <span className="max-w-full truncate text-[1.25cqw] leading-tight font-semibold opacity-90">
            {levelLabel(t, index + 1, level.title)}
            {level.beaten && <span aria-hidden> 🏆</span>}
            {!level.unlocked && <span aria-hidden> 🔒</span>}
          </span>
          <span id={`island-${level.id}-state`} className="sr-only">
            {level.beaten
              ? t('kids.map.islandBeaten')
              : level.unlocked
                ? t('kids.map.islandOpen')
                : t('kids.map.islandLockedState')}
          </span>
          {level.description && (
            <span className="line-clamp-2 font-heading text-[1.8cqw] leading-tight font-bold">{level.description}</span>
          )}
        </span>
        {next && (
          <span aria-hidden className="kids-hint absolute -top-[22%] text-[2.6cqw]">
            👇
          </span>
        )}
        <span className="kids-bob relative block w-full flex-1">
          <IslandArt index={index} boss={level.boss !== null} />
          {!level.unlocked && (
            <span
              aria-hidden
              className="absolute inset-[-10%] rounded-[50%] bg-[radial-gradient(ellipse,rgba(244,251,255,0.92)_35%,transparent_72%)]"
            />
          )}
        </span>
      </motion.button>
    </li>
  )
}
