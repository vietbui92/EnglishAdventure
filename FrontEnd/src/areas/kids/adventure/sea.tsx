import { spread } from '@/areas/kids/adventure/Layer'

/*
 * Life on the misty sea (owner 2026-10-24: "more mysterious: mist, waves, birds in the sky, fish in the sea").
 * Decorative SVG; the kids-* animations in kids.css stop with "reduce motion".
 */

/** A steady unevenness, so repeated things do not line up. */
const jitter = (x: number, range: number) => (((Math.abs(x) * 7919) % 97) / 97 - 0.5) * range

/** Banks of mist drifting over the sea, thick or thin. */
export function SeaMist({ from, to, y, opacity }: { from: number; to: number; y: number; opacity: number }) {
  return (
    <g className="kids-drift-slow">
      {spread(from, to, 260).map((x) => (
        <ellipse key={x} cx={x + 130} cy={y + jitter(x, 24)} rx={230} ry={30} fill="#f4fbff" opacity={opacity} />
      ))}
    </g>
  )
}

/** Seagulls gliding across the sky, flapping now and then. */
export function Gulls({ from, to, top }: { from: number; to: number; top: number }) {
  return (
    <g>
      {spread(from, to, 480).map((x, index) => (
        <g key={x} transform={`translate(${x + jitter(x, 200)} ${top + Math.abs(jitter(x * 3, 1)) * 70})`}>
          <g
            className="kids-glide"
            style={{
              animationDuration: `${26 + (Math.abs(index) % 4) * 6}s`,
              animationDelay: `-${(Math.abs(index) % 5) * 5}s`,
            }}
          >
            <g className="kids-flap" style={{ animationDelay: `${(Math.abs(index) % 3) * 0.3}s` }}>
              <path
                d="M-16 0 Q-8 -9 0 0 Q8 -9 16 0"
                stroke="#ffffff"
                strokeWidth="3.5"
                fill="none"
                strokeLinecap="round"
              />
            </g>
          </g>
        </g>
      ))}
    </g>
  )
}

/** Dark fish shapes swimming just under the surface. */
export function FishShadows({ from, to, top, bottom }: { from: number; to: number; top: number; bottom: number }) {
  return (
    <g fill="#0b3f63" opacity="0.28">
      {spread(from, to, 300).map((x, index) => (
        <g key={x} transform={`translate(${x + jitter(x, 120)} ${top + Math.abs(jitter(x * 5, 1)) * (bottom - top)})`}>
          <g className="kids-swim" style={{ animationDelay: `-${(Math.abs(index) % 6) * 1.5}s` }}>
            <ellipse cx="0" cy="0" rx="22" ry="8" />
            <path d="M18 0 L32 -9 L32 9 Z" />
          </g>
        </g>
      ))}
    </g>
  )
}

/** Fish leaping out of the water in an arc, each on its own time. */
export function LeapingFish({ from, to, top, bottom }: { from: number; to: number; top: number; bottom: number }) {
  return (
    <g>
      {spread(from, to, 420).map((x, index) => (
        <g key={x} transform={`translate(${x + jitter(x, 160)} ${top + Math.abs(jitter(x * 7, 1)) * (bottom - top)})`}>
          <g className="kids-leap" style={{ animationDelay: `-${(Math.abs(index) % 5) * 1.7}s` }}>
            <ellipse cx="0" cy="0" rx="13" ry="6" fill="#ff9f43" />
            <path d="M11 0 L21 -7 L21 7 Z" fill="#ff9f43" />
            <circle cx="-7" cy="-1.5" r="1.6" fill="#1b1b1b" />
          </g>
          <ellipse cx="22" cy="6" rx="18" ry="4" fill="#ffffff" opacity="0.5" className="kids-ripple" />
        </g>
      ))}
    </g>
  )
}

/** Bigger rolling waves with white foam, for the near sea. */
export function FoamWaves({ from, to, y }: { from: number; to: number; y: number }) {
  const start = Math.floor(from / 120) * 120 - 120
  return (
    <g className="kids-wave">
      {Array.from({ length: Math.ceil((to - start) / 120) + 2 }, (_, index) => {
        const x = start + index * 120
        return (
          <g key={x}>
            <path d={`M${x} ${y} q30 -22 60 0 q30 22 60 0 V${y + 40} H${x} Z`} fill="#2a7fb8" opacity="0.5" />
            <path d={`M${x} ${y} q30 -22 60 0`} stroke="#ffffff" strokeWidth="5" fill="none" strokeLinecap="round" />
          </g>
        )
      })}
    </g>
  )
}
