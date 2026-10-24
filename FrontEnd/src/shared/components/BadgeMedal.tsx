import { Lock } from 'lucide-react'
import { useState } from 'react'
import { cn } from '@/shared/lib/utils'

/**
 * The drawn medals of the four badges (S5.2, PD-011), used when the Admin has not linked a picture. The kinds are the
 * backend's BadgeKind names.
 */
const medals: Record<string, { emoji: string; tone: string }> = {
  FirstLesson: { emoji: '🌱', tone: 'from-lime-300 to-emerald-500' },
  Perfect: { emoji: '⭐', tone: 'from-yellow-200 to-amber-500' },
  LevelConqueror: { emoji: '🏆', tone: 'from-orange-300 to-red-500' },
  LevelSuperstar: { emoji: '👑', tone: 'from-fuchsia-300 to-violet-600' },
}

const sizes = { sm: 'size-12 text-2xl', md: 'size-20 text-4xl', lg: 'size-28 text-6xl' }

/**
 * A badge's medal: the Admin's picture, or the drawn one for its kind. A badge not won yet is grey, with a lock.
 * Decorative: the badge's name is always shown next to it.
 */
export function BadgeMedal({
  kind,
  imageUrl,
  earned = true,
  size = 'md',
  className,
}: {
  kind: string
  imageUrl?: string | null
  earned?: boolean
  size?: keyof typeof sizes
  className?: string
}) {
  const [broken, setBroken] = useState(false)
  const medal = medals[kind] ?? { emoji: '🏅', tone: 'from-sky-300 to-blue-500' }

  return (
    <span
      aria-hidden
      className={cn(
        'relative inline-flex shrink-0 items-center justify-center rounded-full border-4 border-white bg-gradient-to-br shadow-md select-none',
        medal.tone,
        !earned && 'opacity-60 grayscale',
        sizes[size],
        className,
      )}
    >
      {imageUrl && !broken ? (
        <img
          src={imageUrl}
          alt=""
          referrerPolicy="no-referrer"
          onError={() => setBroken(true)}
          className="size-full rounded-full object-cover"
        />
      ) : (
        <span className="leading-none">{medal.emoji}</span>
      )}
      {!earned && (
        <span className="absolute -right-1 -bottom-1 flex size-1/3 min-h-5 min-w-5 items-center justify-center rounded-full bg-slate-600 text-white">
          <Lock className="size-1/2" />
        </span>
      )}
    </span>
  )
}
