import { motion, useTransform, type MotionValue } from 'motion/react'
import type { ReactNode } from 'react'
import { cn } from '@/shared/lib/utils'

/** A scene is 1000 × 600 units of its world on the stage; worlds that scroll are wider. */
export const VIEW = 1000
export const HEIGHT = 600

/** How far a layer keeps drawing past the world's ends, for screens wider than the stage (see Stage). */
const BLEED = VIEW

/** World units as a share of the world's width or height, for placing HTML on the drawing. */
export const across = (x: number, width: number) => `${(x / width) * 100}%`
export const down = (y: number) => `${(y / HEIGHT) * 100}%`

/** The camera's left edge for keeping `x` at `lead` from the left, within the world. */
export const follow = (x: number, world: number, lead = VIEW / 2) => Math.min(Math.max(x - lead, 0), world - VIEW)

/**
 * A layer of a scrolling world, moved by the camera at its own depth: 1 moves with the world, less is farther away,
 * more is nearer. `drawing` draws in SVG from `from` to `to` (past both ends of the layer, so wide screens see no
 * gap); children are HTML placed on the layer itself, in shares of its width (see `across`).
 */
export function Layer({
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
  drawing: (from: number, to: number) => ReactNode
  children?: ReactNode
}) {
  const width = VIEW + (world - VIEW) * depth
  const x = useTransform(camera, (left) => `${(-(left * depth) / width) * 100}%`)

  return (
    <motion.div className={cn('absolute inset-y-0 left-0', className)} style={{ width: `${(width / VIEW) * 100}%`, x }}>
      <svg
        viewBox={`${-BLEED} 0 ${width + 2 * BLEED} ${HEIGHT}`}
        className="absolute inset-y-0 max-w-none"
        style={{ left: `${(-BLEED / width) * 100}%`, width: `${((width + 2 * BLEED) / width) * 100}%`, height: '100%' }}
        aria-hidden
      >
        {drawing(-BLEED, width + BLEED)}
      </svg>
      {children}
    </motion.div>
  )
}

/** Positions from `from` to `to`, `step` apart, for things a layer repeats (clouds, trees, waves). */
export function spread(from: number, to: number, step: number) {
  const first = Math.floor(from / step) * step
  return Array.from({ length: Math.ceil((to - first) / step) + 1 }, (_, index) => first + index * step)
}
