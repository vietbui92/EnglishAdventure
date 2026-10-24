import { motion, useAnimate, useReducedMotion } from 'motion/react'

/*
 * Small rewards and nudges for the games (owner 2026-10-24: lessons should not feel dry): a burst of confetti, a pop
 * for a right answer, a shake for a wrong one. None of them run when the device asks for less motion.
 */

const colors = ['#f59e0b', '#ef4444', '#22c55e', '#3b82f6', '#a855f7', '#ec4899']

/** Confetti and stars flying out from the middle of the parent (which must be `relative`). */
export function Burst({ big = false }: { big?: boolean }) {
  const still = useReducedMotion()
  if (still) return null

  const count = big ? 24 : 12
  const reach = big ? 220 : 80
  return (
    <span aria-hidden className="pointer-events-none absolute inset-0 z-20 flex items-center justify-center">
      {Array.from({ length: count }, (_, index) => {
        // Spread evenly, with a little made-up unevenness so it does not look like a clock.
        const angle = (index / count) * Math.PI * 2 + ((index * 7) % 5) * 0.08
        const distance = reach * (0.6 + ((index * 13) % 7) / 14)
        const star = index % 3 === 0
        return (
          <motion.span
            key={index}
            initial={{ x: 0, y: 0, scale: 0, opacity: 1, rotate: 0 }}
            animate={{
              x: Math.cos(angle) * distance,
              y: Math.sin(angle) * distance + (big ? 60 : 20),
              scale: [0, 1.2, 0.9],
              opacity: [1, 1, 0],
              rotate: index % 2 ? 180 : -180,
            }}
            transition={{ duration: big ? 1.6 : 0.9, ease: 'easeOut' }}
            className="absolute"
          >
            {star ? (
              <span className="text-2xl">⭐</span>
            ) : (
              <span className="block size-3 rounded-sm" style={{ backgroundColor: colors[index % colors.length] }} />
            )}
          </motion.span>
        )
      })}
    </span>
  )
}

/** Pops and shakes elements inside `scope`, found by a CSS selector. */
export function useNudges() {
  const [scope, animate] = useAnimate()
  const still = useReducedMotion()

  return {
    scope,
    pop: (selector: string) => {
      if (!still) void animate(selector, { scale: [1, 1.15, 0.95, 1] }, { duration: 0.45 })
    },
    shake: (selector: string) => {
      if (!still) void animate(selector, { x: [0, -12, 12, -8, 8, -4, 0] }, { duration: 0.45 })
    },
  }
}
