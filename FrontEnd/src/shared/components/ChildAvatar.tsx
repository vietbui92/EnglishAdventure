import type { ParseKeys } from 'i18next'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/utils'

/**
 * The drawn avatars of child profiles. The keys are the backend's ChildProfile.AvatarKeys (also served by
 * GET /api/v1/family/children/options); a new key needs an entry here.
 */
const avatars: Record<string, { emoji: string; tone: string }> = {
  fox: { emoji: '🦊', tone: 'bg-orange-100' },
  owl: { emoji: '🦉', tone: 'bg-amber-100' },
  cat: { emoji: '🐱', tone: 'bg-yellow-100' },
  bear: { emoji: '🐻', tone: 'bg-stone-200' },
  rabbit: { emoji: '🐰', tone: 'bg-pink-100' },
  panda: { emoji: '🐼', tone: 'bg-slate-100' },
  lion: { emoji: '🦁', tone: 'bg-amber-200' },
  penguin: { emoji: '🐧', tone: 'bg-sky-100' },
  elephant: { emoji: '🐘', tone: 'bg-indigo-100' },
  monkey: { emoji: '🐵', tone: 'bg-lime-100' },
  koala: { emoji: '🐨', tone: 'bg-teal-100' },
  turtle: { emoji: '🐢', tone: 'bg-emerald-100' },
}

export const avatarKeys = Object.keys(avatars)

const sizes = { sm: 'size-10 text-xl', md: 'size-16 text-3xl', lg: 'size-24 text-5xl' }

/**
 * A child's avatar. Decorative by default (next to the nickname); with `labelled` it carries the animal's name, for
 * example in the avatar picker.
 */
export function ChildAvatar({
  avatarKey,
  size = 'md',
  labelled = false,
  className,
}: {
  avatarKey: string
  size?: keyof typeof sizes
  labelled?: boolean
  className?: string
}) {
  const { t } = useTranslation()
  const avatar = avatars[avatarKey] ?? { emoji: '🙂', tone: 'bg-muted' }
  const label = avatars[avatarKey] ? t(`avatars.${avatarKey}` as ParseKeys) : avatarKey

  return (
    <span
      role={labelled ? 'img' : undefined}
      aria-label={labelled ? label : undefined}
      aria-hidden={labelled ? undefined : true}
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-full select-none',
        avatar.tone,
        sizes[size],
        className,
      )}
    >
      {avatar.emoji}
    </span>
  )
}
