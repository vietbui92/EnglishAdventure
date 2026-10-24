import { ImageOff } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * A word's picture as a small square, or a placeholder when there is none or it does not load. The picture lives on
 * another site (PD-012: URLs first), so no referrer is sent to it.
 */
export function WordPicture({ url, alt }: { url: string | null; alt: string }) {
  const { t } = useTranslation()
  const [failedUrl, setFailedUrl] = useState<string | null>(null)

  if (!url || failedUrl === url) {
    return (
      <span
        role="img"
        aria-label={url ? t('admin.content.words.pictureBroken') : t('admin.content.words.noPicture')}
        className="flex size-12 shrink-0 items-center justify-center rounded-md border border-dashed text-muted-foreground"
      >
        <ImageOff aria-hidden className="size-5" />
      </span>
    )
  }

  return (
    <img
      src={url}
      alt={alt}
      loading="lazy"
      referrerPolicy="no-referrer"
      onError={() => setFailedUrl(url)}
      className="size-12 shrink-0 rounded-md border object-cover"
    />
  )
}
