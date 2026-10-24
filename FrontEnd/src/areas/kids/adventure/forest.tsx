import { motion } from 'motion/react'
import { spread } from '@/areas/kids/adventure/Layer'
import { cn } from '@/shared/lib/utils'

/*
 * The mysterious forest of the discovery walk (owner 2026-10-24: "like walking into a forest, more mysterious").
 * Deep greens, light falling through the leaves, mist and fireflies, in layers at different depths. Every piece is
 * decorative SVG; the kids-* animations in kids.css stop with "reduce motion".
 */

/** A made-up but steady unevenness, so repeated trees do not line up like fence posts. */
const jitter = (x: number, range: number) => (((Math.abs(x) * 7919) % 97) / 97 - 0.5) * range

/** The far canopy: dark treetops against the dim sky, and thin trunks below them. */
export function FarCanopy({ from, to }: { from: number; to: number }) {
  return (
    <g>
      {spread(from, to, 180).map((x) => (
        <g key={x}>
          <rect x={x + 60 + jitter(x, 40)} y={120} width={22} height={300} fill="#1a4a40" />
          <circle cx={x + 70 + jitter(x, 50)} cy={110 + jitter(x * 3, 40)} r={110} fill="#143d36" />
        </g>
      ))}
      <rect x={from} y={-400} width={to - from} height={440} fill="#123832" />
    </g>
  )
}

/** Shafts of sunlight falling through the leaves, glowing softly. */
export function LightRays({ from, to }: { from: number; to: number }) {
  return (
    <g>
      <defs>
        <linearGradient id="forest-ray" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff6c4" stopOpacity="0.32" />
          <stop offset="1" stopColor="#fff6c4" stopOpacity="0" />
        </linearGradient>
      </defs>
      {spread(from, to, 420).map((x, index) => (
        <path
          key={x}
          d={`M${x + 120} -20 L${x + 190} -20 L${x + 330} 560 L${x + 170} 560 Z`}
          fill="url(#forest-ray)"
          className="kids-shimmer"
          style={{ animationDelay: `${(Math.abs(index) % 4) * 0.7}s` }}
        />
      ))}
    </g>
  )
}

/** Middle trees: thick mossy trunks with vines hanging from the canopy. */
export function MiddleTrees({ from, to }: { from: number; to: number }) {
  return (
    <g>
      {spread(from, to, 340).map((x) => {
        const trunk = x + 100 + jitter(x, 120)
        return (
          <g key={x}>
            <path
              d={`M${trunk} 440 Q${trunk + 10} 250 ${trunk - 6} -10 L${trunk + 54} -10 Q${trunk + 44} 250 ${trunk + 64} 440 Z`}
              fill="#24533f"
            />
            <ellipse cx={trunk + 30} cy={300} rx={22} ry={8} fill="#3d7a4f" opacity="0.8" />
            <ellipse cx={trunk + 22} cy={180} rx={16} ry={6} fill="#3d7a4f" opacity="0.7" />
            <g className="kids-sway" style={{ transformOrigin: `${trunk + 140}px 0px` }}>
              <path
                d={`M${trunk + 140} -10 q-14 80 6 150 q12 50 -4 110`}
                stroke="#2f6b45"
                strokeWidth="5"
                fill="none"
                strokeLinecap="round"
              />
              <circle cx={trunk + 142} cy={250} r={8} fill="#4f9a5c" />
              <circle cx={trunk + 138} cy={140} r={6} fill="#4f9a5c" />
            </g>
          </g>
        )
      })}
      <path
        d={`M${from} 60 ${spread(from, to, 120)
          .map((x) => `Q${x + 60} ${110 + jitter(x, 50)} ${x + 120} 60`)
          .join(' ')} L${to} -400 L${from} -400 Z`}
        fill="#1b4a38"
      />
    </g>
  )
}

/** Low mist drifting between the trees. */
export function Mist({ from, to, y, opacity = 0.14 }: { from: number; to: number; y: number; opacity?: number }) {
  return (
    <g className="kids-drift-slow">
      {spread(from, to, 300).map((x) => (
        <ellipse key={x} cx={x + 150} cy={y + jitter(x, 30)} rx={240} ry={38} fill="#e8fff4" opacity={opacity} />
      ))}
    </g>
  )
}

/** The forest floor with its winding earth path, stones and ferns. */
export function ForestFloor({ from, to, path }: { from: number; to: number; path: { top: number; bottom: number } }) {
  return (
    <g>
      <rect x={from} y={path.top - 40} width={to - from} height={800} fill="#1f4a32" />
      <rect x={from} y={path.top} width={to - from} height={path.bottom - path.top} fill="#6b4f34" />
      <rect x={from} y={path.top} width={to - from} height={8} fill="#2c5a3c" opacity="0.7" />
      {spread(from, to, 90).map((x) => (
        <ellipse
          key={x}
          cx={x + 40 + jitter(x, 40)}
          cy={path.top + 20 + Math.abs(jitter(x * 5, 1)) * (path.bottom - path.top - 30)}
          rx={10}
          ry={5}
          fill="#8a6b4a"
        />
      ))}
      {spread(from, to, 150).map((x) => (
        <Fern key={x} x={x + 70 + jitter(x, 60)} y={path.top + 4} size={0.8 + Math.abs(jitter(x, 0.6))} />
      ))}
      {spread(from, to, 170).map((x) => (
        <Fern key={`low-${x}`} x={x + 20 + jitter(x * 2, 60)} y={path.bottom + 40} size={1.1} />
      ))}
    </g>
  )
}

export function Fern({ x, y, size = 1 }: { x: number; y: number; size?: number }) {
  return (
    <g transform={`translate(${x} ${y}) scale(${size})`}>
      <g className="kids-sway" style={{ transformOrigin: '0px 0px' }}>
        <path d="M0 0 Q-30 -20 -48 -6 Q-24 -12 0 0" fill="#2f7a46" />
        <path d="M0 0 Q30 -24 50 -10 Q26 -14 0 0" fill="#3a8a50" />
        <path d="M0 0 Q-8 -40 -24 -50 Q-6 -32 0 0" fill="#3a8a50" />
        <path d="M0 0 Q10 -42 28 -52 Q8 -30 0 0" fill="#2f7a46" />
      </g>
    </g>
  )
}

/** A forest pond with lily pads, for a word that swims. */
export function Pond({ x, y }: { x: number; y: number }) {
  return (
    <g>
      <ellipse cx={x} cy={y} rx={170} ry={46} fill="#2a6f86" stroke="#4f9a5c" strokeWidth="10" />
      <ellipse cx={x - 30} cy={y - 8} rx={110} ry={20} fill="#3f8fa8" opacity="0.6" className="kids-shimmer" />
      <ellipse cx={x + 110} cy={y + 10} rx={22} ry={8} fill="#4f9a5c" />
      <ellipse cx={x - 120} cy={y + 4} rx={18} ry={7} fill="#4f9a5c" />
    </g>
  )
}

/** Fireflies floating and blinking, each on its own time. */
export function Fireflies({ from, to }: { from: number; to: number }) {
  return (
    <g>
      {spread(from, to, 70).map((x, index) => (
        <circle
          key={x}
          cx={x + jitter(x, 60)}
          cy={140 + Math.abs(jitter(x * 3, 1)) * 320}
          r={3.5}
          fill="#fff59d"
          className="kids-firefly"
          style={{ animationDelay: `${(Math.abs(index) % 7) * 0.6}s`, filter: 'drop-shadow(0 0 4px #fff59d)' }}
        />
      ))}
    </g>
  )
}

/**
 * Leaves right in front of the camera, passing quickly for depth. They only frame the edges: low ferns below the
 * path and a few leaves hanging at the top, never across the middle where the explorer and the words are (owner
 * 2026-10-24: tall trunks in front hid the pictures).
 */
export function NearLeaves({ from, to }: { from: number; to: number }) {
  return (
    <g fill="#0c2a1c" opacity="0.85">
      {spread(from, to, 520).map((x) => (
        <g key={x}>
          <path d={`M${x} 600 Q${x + 50} 540 ${x + 140} 530 Q${x + 110} 570 ${x + 170} 600 Z`} />
          <path d={`M${x + 220} 600 Q${x + 280} 550 ${x + 360} 548 Q${x + 320} 578 ${x + 350} 600 Z`} fill="#123a26" />
          <path d={`M${x + 380} -10 Q${x + 410} 30 ${x + 480} 40 Q${x + 450} 10 ${x + 480} -10 Z`} />
        </g>
      ))}
    </g>
  )
}

/**
 * The bush a word hides in, with a pair of curious eyes. It rustles when the explorer comes close, then its two
 * halves part to let the word out.
 */
export function HidingBush({ rustling, open }: { rustling: boolean; open: boolean }) {
  const half = (side: -1 | 1) => (
    <motion.g
      initial={false}
      animate={open ? { x: side * 140, rotate: side * 18, opacity: 0 } : { x: 0, rotate: 0, opacity: 1 }}
      transition={{ duration: 0.6, ease: 'easeIn' }}
      style={{ transformOrigin: `${100 + side * 60}px 160px` }}
    >
      <circle cx={100 + side * 40} cy={110} r={56} fill="#2f7a46" />
      <circle cx={100 + side * 70} cy={140} r={44} fill="#276b3d" />
      <circle cx={100 + side * 20} cy={70} r={42} fill="#3a8a50" />
    </motion.g>
  )
  return (
    <svg viewBox="0 0 200 180" className={cn('size-full overflow-visible', rustling && 'kids-rustle')} aria-hidden>
      {!open && (
        <g className="kids-blink" style={{ transformOrigin: '100px 105px' }}>
          <ellipse cx="84" cy="105" rx="9" ry="11" fill="#fff59d" />
          <ellipse cx="116" cy="105" rx="9" ry="11" fill="#fff59d" />
          <circle cx="86" cy="107" r="4" fill="#1b1b1b" />
          <circle cx="118" cy="107" r="4" fill="#1b1b1b" />
        </g>
      )}
      {half(-1)}
      {half(1)}
      {!open && (
        <text x="150" y="40" fontSize="38" fontWeight="bold" fill="#fff59d" className="kids-hint">
          ?
        </text>
      )}
    </svg>
  )
}
