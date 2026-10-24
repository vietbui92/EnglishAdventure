import { Volume2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { speak } from '@/areas/kids/adventure/sound'
import { Button } from '@/shared/components/ui/button'

/** Says an English word aloud with the device's voice. */
export function ListenButton({ text }: { text: string }) {
  const { t } = useTranslation()
  return (
    <Button
      variant="secondary"
      size="icon"
      className="size-12 shrink-0 rounded-full"
      aria-label={t('kids.play.listen')}
      onClick={() => speak(text)}
    >
      <Volume2 aria-hidden className="size-6" />
    </Button>
  )
}
