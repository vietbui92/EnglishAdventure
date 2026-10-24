import { ImageOff } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/utils'

/**
 * A picture from the lesson content (PD-012: pictures are links to other sites, so no referrer is sent). When it is
 * missing or does not load, a soft placeholder keeps the card's shape.
 */
export function KidsPicture({
  url,
  alt,
  className,
}: {
  url: string | null | undefined
  alt: string
  className?: string
}) {
  const { t } = useTranslation()
  const [failedUrl, setFailedUrl] = useState<string | null>(null)

  if (!url || failedUrl === url) {
    return (
      <span
        role="img"
        aria-label={t('kids.play.noPicture')}
        className={cn('flex items-center justify-center rounded-3xl bg-muted text-muted-foreground', className)}
      >
        <ImageOff aria-hidden className="size-1/3" />
      </span>
    )
  }

  return (
    <img
      src={url}
      alt={alt}
      referrerPolicy="no-referrer"
      onError={() => setFailedUrl(url)}
      className={cn('rounded-3xl bg-card object-contain', className)}
    />
  )
}
