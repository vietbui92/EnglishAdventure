import { animate, type MotionValue } from 'motion/react'

/** A point of a scene, in its own units (a scene is 1000 wide and 600 high on screen). */
export interface Point {
  x: number
  y: number
}

/** A route through points, walked at an even speed: `at(0)` is its start, `at(1)` its end. */
export function track(points: Point[]) {
  const lengths = [0]
  for (let i = 1; i < points.length; i++) {
    lengths.push(lengths[i - 1]! + Math.hypot(points[i]!.x - points[i - 1]!.x, points[i]!.y - points[i - 1]!.y))
  }
  const length = lengths.at(-1)!

  return {
    length,
    at(fraction: number): Point {
      const distance = Math.min(Math.max(fraction, 0), 1) * length
      const index = Math.max(
        1,
        lengths.findIndex((at) => at >= distance),
      )
      const from = points[index - 1]!
      const to = points[index] ?? from
      const span = lengths[index]! - lengths[index - 1]! || 1
      const part = (distance - lengths[index - 1]!) / span
      return { x: from.x + (to.x - from.x) * part, y: from.y + (to.y - from.y) * part }
    },
  }
}

/** A smooth curve through the given points (Catmull-Rom), as many short straight pieces. */
export function smooth(points: Point[], pieces = 12): Point[] {
  const result: Point[] = []
  for (let i = 0; i < points.length - 1; i++) {
    const p0 = points[i - 1] ?? points[i]!
    const p1 = points[i]!
    const p2 = points[i + 1]!
    const p3 = points[i + 2] ?? p2
    for (let step = 0; step < pieces; step++) {
      const t = step / pieces
      const t2 = t * t
      const t3 = t2 * t
      const blend = (a: number, b: number, c: number, d: number) =>
        0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3)
      result.push({ x: blend(p0.x, p1.x, p2.x, p3.x), y: blend(p0.y, p1.y, p2.y, p3.y) })
    }
  }
  result.push(points.at(-1)!)
  return result
}

/** A sea crossing from one dock to another, bowing out into open water. */
export function crossing(from: Point, to: Point, bow = 70): Point[] {
  const middle = { x: (from.x + to.x) / 2, y: Math.max(from.y, to.y) + bow }
  return smooth([from, middle, to])
}

/** An SVG path through the points. */
export function svgPath(points: Point[]) {
  return points.map((point, index) => `${index ? 'L' : 'M'}${point.x.toFixed(1)} ${point.y.toFixed(1)}`).join(' ')
}

/**
 * Animates a value, or sets it at once when the child's device asks for less motion (and in tests): every journey
 * still ends in the same place, only without the trip.
 */
export function glide(
  value: MotionValue<number>,
  to: number,
  seconds: number,
  still: boolean,
  ease: 'linear' | 'easeIn' | 'easeInOut' = 'easeInOut',
) {
  if (still || seconds <= 0) {
    value.set(to)
    return Promise.resolve()
  }
  return new Promise<void>((resolve) => {
    animate(value, to, { duration: seconds, ease, onComplete: resolve })
  })
}

/** Waits, or not at all with less motion. */
export function pause(seconds: number, still: boolean) {
  return still ? Promise.resolve() : new Promise<void>((resolve) => setTimeout(resolve, seconds * 1000))
}
