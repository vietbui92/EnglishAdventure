import { avatarOf } from '@/shared/components/ChildAvatar'
import { cn } from '@/shared/lib/utils'

/*
 * The drawn pieces of the adventure (owner 2026-10-24: a sea of islands, 2.5D). All are decorative SVG; what the
 * child can tap is labelled by the scenes. Their animations are the kids-* classes in kids.css, which do nothing when
 * the device asks for less motion.
 */

export interface IslandTheme {
  grass: string
  shade: string
  hill: string
  sand: string
  decor: 'palm' | 'autumn' | 'snow' | 'cactus' | 'candy' | 'crystal'
}

/** Each level's island looks different, in this order, starting again after the last. */
export const islandThemes: IslandTheme[] = [
  { grass: '#7ccf6b', shade: '#55ad59', hill: '#3f8f4e', sand: '#f6dc9c', decor: 'palm' },
  { grass: '#f2b35b', shade: '#d98b3a', hill: '#b86a2c', sand: '#f8e2b0', decor: 'autumn' },
  { grass: '#f1f8ff', shade: '#c9e0f7', hill: '#9cc0e8', sand: '#e6eef5', decor: 'snow' },
  { grass: '#efcf74', shade: '#d6aa4c', hill: '#c08a38', sand: '#fbe7b5', decor: 'cactus' },
  { grass: '#f7aad0', shade: '#e382b5', hill: '#c95e98', sand: '#fde3ef', decor: 'candy' },
  { grass: '#ab8fe2', shade: '#8a6bcb', hill: '#6a4aa8', sand: '#e8dcfb', decor: 'crystal' },
]

export const themeOf = (index: number) => islandThemes[index % islandThemes.length]!

/** One tree (or cactus, lollipop, crystal) of an island's kind, standing on (x, y). */
export function Decor({ kind, x, y, size = 1 }: { kind: IslandTheme['decor']; x: number; y: number; size?: number }) {
  const transform = `translate(${x} ${y}) scale(${size})`
  switch (kind) {
    case 'palm':
      return (
        <g transform={transform}>
          <path d="M0 0 Q-6 -30 4 -62" stroke="#9a6a3a" strokeWidth="7" fill="none" strokeLinecap="round" />
          <g className="kids-sway" style={{ transformOrigin: '4px -62px' }}>
            <path d="M4 -62 Q-26 -78 -40 -56 Q-22 -66 4 -62" fill="#3fa34d" />
            <path d="M4 -62 Q34 -80 46 -54 Q26 -66 4 -62" fill="#4cb85a" />
            <path d="M4 -62 Q-8 -92 -28 -88 Q-12 -80 4 -62" fill="#55c463" />
            <path d="M4 -62 Q22 -94 40 -84 Q20 -78 4 -62" fill="#3fa34d" />
            <circle cx="2" cy="-58" r="4" fill="#7a4b22" />
            <circle cx="8" cy="-57" r="4" fill="#7a4b22" />
          </g>
        </g>
      )
    case 'autumn':
      return (
        <g transform={transform}>
          <rect x="-4" y="-30" width="8" height="30" rx="3" fill="#8a5a33" />
          <circle cx="0" cy="-44" r="22" fill="#e8742f" />
          <circle cx="-12" cy="-36" r="14" fill="#f39a3d" />
          <circle cx="12" cy="-38" r="14" fill="#d95f2a" />
        </g>
      )
    case 'snow':
      return (
        <g transform={transform}>
          <rect x="-4" y="-14" width="8" height="14" fill="#7a5a3a" />
          <path d="M0 -70 L24 -14 L-24 -14 Z" fill="#3c8f6a" />
          <path d="M0 -70 L12 -42 Q0 -36 -12 -42 Z" fill="#ffffff" />
          <path d="M-18 -28 Q0 -22 18 -28 L24 -14 L-24 -14 Z" fill="#ffffff" opacity="0.8" />
        </g>
      )
    case 'cactus':
      return (
        <g transform={transform}>
          <rect x="-8" y="-58" width="16" height="58" rx="8" fill="#4fae5b" />
          <path
            d="M-8 -30 H-18 Q-24 -30 -24 -38 V-46"
            stroke="#4fae5b"
            strokeWidth="9"
            fill="none"
            strokeLinecap="round"
          />
          <path d="M8 -22 H18 Q24 -22 24 -30 V-40" stroke="#4fae5b" strokeWidth="9" fill="none" strokeLinecap="round" />
          <circle cx="0" cy="-60" r="5" fill="#f26b9a" />
        </g>
      )
    case 'candy':
      return (
        <g transform={transform}>
          <rect x="-3" y="-40" width="6" height="40" rx="3" fill="#ffffff" />
          <circle cx="0" cy="-52" r="18" fill="#ff7eb6" />
          <path d="M-12 -58 Q0 -66 10 -56 Q2 -48 -8 -50" stroke="#fff" strokeWidth="4" fill="none" />
        </g>
      )
    case 'crystal':
      return (
        <g transform={transform}>
          <path d="M0 -60 L14 -20 L0 0 L-14 -20 Z" fill="#7fe3ff" />
          <path d="M0 -60 L14 -20 L0 -14 Z" fill="#c4f3ff" />
          <path d="M-20 -30 L-12 -8 L-22 0 L-30 -10 Z" fill="#a98bff" />
        </g>
      )
  }
}

/**
 * An island as seen from the sea, drawn in a 400 × 260 box: shallow water, beach, hills, its trees, and the
 * mountain where its boss lives (with a flag when there is one).
 */
export function IslandArt({ index, boss }: { index: number; boss: boolean }) {
  const theme = themeOf(index)
  return (
    <svg viewBox="0 0 400 260" className="size-full overflow-visible" aria-hidden>
      <ellipse cx="200" cy="200" rx="196" ry="48" fill="#a7efe9" opacity="0.75" className="kids-shimmer" />
      <ellipse cx="200" cy="190" rx="168" ry="38" fill={theme.sand} />
      <path d="M190 120 L262 28 L330 118 Z" fill={theme.hill} />
      <path d="M262 28 L282 54 Q266 60 252 50 Z" fill="#ffffff" opacity="0.7" />
      {boss && (
        <g className="kids-sway" style={{ transformOrigin: '262px 28px' }}>
          <line x1="262" y1="28" x2="262" y2="0" stroke="#5b4636" strokeWidth="4" />
          <path d="M262 2 L292 10 L262 18 Z" fill="#ef4444" />
        </g>
      )}
      <path d="M48 188 Q58 120 140 108 Q200 54 268 100 Q344 106 354 188 Z" fill={theme.grass} />
      <path d="M48 188 Q120 160 200 172 Q290 160 354 188 Z" fill={theme.shade} />
      <Decor kind={theme.decor} x={110} y={150} />
      <Decor kind={theme.decor} x={300} y={160} size={0.85} />
      <Decor kind={theme.decor} x={190} y={128} size={0.7} />
    </svg>
  )
}

/** The mainland where every adventure starts: a hill with the child's house, a lighthouse and a wooden dock. */
export function Mainland() {
  return (
    <g aria-hidden>
      <path d="M0 196 Q140 176 250 236 Q356 300 380 420 Q396 520 330 600 L0 600 Z" fill="#f6dc9c" />
      <path d="M0 214 Q130 198 226 250 Q326 306 346 420 Q356 512 296 600 L0 600 Z" fill="#7ccf6b" />
      <path d="M0 330 Q120 300 230 360 Q300 400 330 480 Q330 560 280 600 L0 600 Z" fill="#55ad59" />
      {/* The house. */}
      <g transform="translate(70 252)">
        <rect x="0" y="30" width="80" height="58" rx="4" fill="#fff4e0" />
        <path d="M-10 34 L40 -6 L90 34 Z" fill="#e85d4a" />
        <rect x="30" y="56" width="20" height="32" rx="3" fill="#a0673a" />
        <rect x="8" y="46" width="16" height="14" rx="2" fill="#8fd8ff" />
        <rect x="56" y="46" width="16" height="14" rx="2" fill="#8fd8ff" />
      </g>
      {/* The lighthouse, its light turning. */}
      <g transform="translate(232 238)">
        <path d="M-14 70 L-9 0 H9 L14 70 Z" fill="#ffffff" />
        <path d="M-12 46 H12 L13 58 H-13 Z M-10 20 H10 L11 32 H-11 Z" fill="#ef4444" />
        <rect x="-11" y="-14" width="22" height="16" rx="3" fill="#fde68a" className="kids-glow" />
        <path d="M-14 -14 L0 -28 L14 -14 Z" fill="#ef4444" />
      </g>
      <Decor kind="palm" x={176} y={340} />
      <Decor kind="palm" x={40} y={420} size={1.2} />
      {/* The dock. */}
      <g fill="#b07a45">
        <rect x="300" y="478" width="110" height="12" rx="3" />
        <rect x="312" y="486" width="7" height="28" />
        <rect x="350" y="486" width="7" height="28" />
        <rect x="390" y="486" width="7" height="28" />
      </g>
    </g>
  )
}

/** A rowing boat, 160 × 100, whose oars swing while it is rowed. */
export function Boat({ rowing }: { rowing: boolean }) {
  return (
    <svg viewBox="0 0 160 100" className="size-full overflow-visible" aria-hidden>
      <ellipse cx="80" cy="82" rx="78" ry="10" fill="#ffffff" opacity="0.55" className={cn(rowing && 'kids-wake')} />
      <g className={cn(rowing && 'kids-row')} style={{ transformOrigin: '50px 56px' }}>
        <line x1="50" y1="56" x2="8" y2="90" stroke="#8a5a33" strokeWidth="5" strokeLinecap="round" />
        <ellipse cx="8" cy="90" rx="9" ry="4" fill="#8a5a33" />
      </g>
      <path d="M6 52 Q80 104 154 52 Z" fill="#c9773d" />
      <path d="M6 52 H154 L150 60 Q80 90 10 60 Z" fill="#e0904f" />
      <rect x="4" y="48" width="152" height="8" rx="4" fill="#8a5a33" />
      <g className={cn(rowing && 'kids-row kids-row-late')} style={{ transformOrigin: '110px 56px' }}>
        <line x1="110" y1="56" x2="152" y2="90" stroke="#8a5a33" strokeWidth="5" strokeLinecap="round" />
        <ellipse cx="152" cy="90" rx="9" ry="4" fill="#8a5a33" />
      </g>
    </svg>
  )
}

/**
 * The child's character: their avatar animal with an explorer's hat. It bounces while walking and breathes while
 * standing still.
 */
export function Explorer({
  avatarKey,
  walking,
  className,
}: {
  avatarKey: string
  walking: boolean
  className?: string
}) {
  const avatar = avatarOf(avatarKey)
  return (
    <span aria-hidden className={cn('relative block', className)}>
      <span className="absolute inset-x-[15%] -bottom-[6%] h-[14%] rounded-[50%] bg-black/20 blur-[1px]" />
      <span
        className={cn(
          'relative flex size-full items-center justify-center rounded-full border-[0.4cqw] border-white shadow-lg',
          avatar.tone,
          walking ? 'kids-walk' : 'kids-breathe',
        )}
      >
        <span className="text-[4.2cqw] leading-none">{avatar.emoji}</span>
        <svg viewBox="0 0 40 20" className="absolute -top-[28%] w-[78%]">
          <ellipse cx="20" cy="15" rx="19" ry="4" fill="#a8743f" />
          <path d="M8 15 Q8 2 20 2 Q32 2 32 15 Z" fill="#c8924f" />
          <rect x="8" y="10" width="24" height="3" fill="#7a4b22" />
        </svg>
      </span>
    </span>
  )
}

/** A white cloud, drifting slowly. */
export function Cloud({ x, y, size = 1, slow = false }: { x: number; y: number; size?: number; slow?: boolean }) {
  return (
    <g transform={`translate(${x} ${y}) scale(${size})`}>
      <g className={slow ? 'kids-drift-slow' : 'kids-drift'}>
        <ellipse cx="0" cy="0" rx="46" ry="18" fill="#ffffff" opacity="0.92" />
        <circle cx="-16" cy="-12" r="20" fill="#ffffff" opacity="0.92" />
        <circle cx="14" cy="-16" r="24" fill="#ffffff" opacity="0.92" />
      </g>
    </g>
  )
}

/** Rows of little waves across a band of sea, from x `from` to `to` and y `top` to `bottom`, rolling sideways. */
export function WaveRows({
  from,
  to,
  top,
  bottom,
  color = '#ffffff',
}: {
  from: number
  to: number
  top: number
  bottom: number
  color?: string
}) {
  const rows: number[] = []
  for (let y = top; y <= bottom; y += 46) rows.push(y)
  const start = Math.floor(from / 140) * 140 - 140
  return (
    <g aria-hidden stroke={color} strokeWidth="3" fill="none" strokeLinecap="round" opacity="0.55">
      {rows.map((y, row) => (
        <g key={y} className={row % 2 ? 'kids-wave' : 'kids-wave-back'}>
          {Array.from({ length: Math.ceil((to - start) / 140) + 2 }, (_, index) => {
            const x = start + index * 140 + (row % 2) * 70
            return <path key={index} d={`M${x} ${y} q12 -9 24 0 q12 9 24 0`} />
          })}
        </g>
      ))}
    </g>
  )
}
