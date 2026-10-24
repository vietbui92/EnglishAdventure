import { useEffect, useRef, useState, type ReactNode } from 'react'
import { HEIGHT, VIEW } from '@/areas/kids/adventure/Layer'
import { cn } from '@/shared/lib/utils'

interface StageProps {
  /** How much of the world must always be seen across, in world units: a narrow screen shrinks the scene to fit. */
  minWidth?: number
  /** Where the sky meets the ground or the sea, in world units, for the colours outside the scene. */
  horizon: number
  sky: [string, string]
  ground: [string, string]
  /** HTML on top of the scene, placed against the screen's edges (greeting, buttons, messages). */
  hud?: ReactNode
  className?: string
  children: ReactNode
}

/**
 * A full-screen scene (owner 2026-10-24: the kids area should fill the screen). The 1000 × 600 scene is scaled to the
 * screen's height, and to its width when a narrow screen needs it; whatever the screen shows beyond it, the scene's
 * own layers keep drawing (a wide screen sees more of the world to the sides), and the sky and ground colours carry on
 * above and below. Inside, 1cqw is still 10 world units.
 */
export function Stage({ minWidth = VIEW, horizon, sky, ground, hud, className, children }: StageProps) {
  const ref = useRef<HTMLDivElement>(null)
  const [size, setSize] = useState({ width: 0, height: 0 })

  useEffect(() => {
    const element = ref.current
    if (!element) return
    const observer = new ResizeObserver(([entry]) => {
      if (entry) setSize({ width: entry.contentRect.width, height: entry.contentRect.height })
    })
    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  const scale = size.width && size.height ? Math.min(size.height / HEIGHT, size.width / minWidth) : 0
  const top = (size.height - HEIGHT * scale) / 2
  const line = top + horizon * scale

  return (
    <div
      ref={ref}
      className={cn('relative size-full overflow-hidden select-none', className)}
      style={{
        background: `linear-gradient(to bottom, ${sky[0]}, ${sky[1]} ${line}px, ${ground[0]} ${line}px, ${ground[1]})`,
      }}
    >
      <div
        className="@container absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2"
        style={{ width: VIEW * scale, height: HEIGHT * scale }}
      >
        {children}
      </div>
      {hud}
    </div>
  )
}
