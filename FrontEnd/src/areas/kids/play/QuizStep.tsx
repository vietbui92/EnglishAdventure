import { motion } from 'motion/react'
import { Check, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Burst, useNudges } from '@/areas/kids/adventure/effects'
import { playSound } from '@/areas/kids/adventure/sound'
import {
  useAnswerQuestion,
  type AttemptView,
  type EarnedRewards,
  type PlayChoice,
  type PlayQuestion,
} from '@/areas/kids/play/api'
import { KidsPicture } from '@/areas/kids/play/KidsPicture'
import { ListenButton } from '@/areas/kids/play/ListenButton'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

interface QuizStepProps {
  attemptId: string
  question: PlayQuestion
  number: number
  count: number
  /** The answer already given in this attempt (after a resume or once answered here). */
  answered: AttemptView['questions'][number] | undefined
  onAnswered: (attempt: AttemptView, rewards?: EarnedRewards | null) => void
  onNext: () => void
}

/**
 * One quiz question (F4.3, owner 2026-10-24): the child picks one option, the server says right or wrong and, after a
 * wrong one, shows the right option. Only the first answer counts, so the options lock once answered. A right answer
 * pops with confetti and a chime; a wrong one shakes.
 */
export function QuizStep({ attemptId, question, number, count, answered, onAnswered, onNext }: QuizStepProps) {
  const { t } = useTranslation()
  const answer = useAnswerQuestion(attemptId)
  const { scope, pop, shake } = useNudges()
  const [celebrate, setCelebrate] = useState<string | null>(null)
  const pictures = question.form === 'WordToPicture'

  const choose = (option: PlayChoice) => {
    playSound('tap')
    answer.mutate(
      { questionId: question.id, optionId: option.id },
      {
        onSuccess: (result) => {
          onAnswered(result.attempt, result.rewards)
          if (result.correct) {
            playSound('correct')
            setCelebrate(option.id)
            pop(`[data-option="${option.id}"]`)
          } else {
            playSound('wrong')
            shake(`[data-option="${option.id}"]`)
          }
        },
      },
    )
  }

  return (
    <motion.div
      ref={scope}
      initial={{ opacity: 0, x: 60 }}
      animate={{ opacity: 1, x: 0 }}
      className="flex w-full flex-col items-center gap-6"
    >
      <p className="text-lg font-semibold text-muted-foreground">{t('kids.game.questionCount', { number, count })}</p>

      <QuestionPrompt question={question} />

      <div role="group" aria-label={t('kids.game.options')} className="grid w-full grid-cols-2 gap-3">
        {question.options.map((option, index) => {
          const chosen = answered?.chosenOptionId === option.id
          const right = answered?.correctOptionId === option.id
          return (
            <motion.button
              key={option.id}
              type="button"
              data-option={option.id}
              disabled={answered !== undefined || answer.isPending}
              onClick={() => choose(option)}
              aria-label={option.text ?? t('kids.game.pictureOption', { number: index + 1 })}
              aria-pressed={chosen}
              whileHover={answered ? undefined : { scale: 1.05, rotate: index % 2 ? 1 : -1 }}
              whileTap={answered ? undefined : { scale: 0.95 }}
              className={cn(
                'relative flex min-h-20 items-center justify-center gap-2 rounded-3xl border-4 bg-card p-3 text-2xl font-bold shadow-sm focus-visible:ring-4 focus-visible:ring-ring focus-visible:outline-none',
                answered && right && 'border-green-500 bg-green-50',
                answered && chosen && !right && 'border-red-400 bg-red-50',
                answered && !right && !chosen && 'opacity-60',
                !answered && 'border-transparent',
              )}
            >
              {celebrate === option.id && <Burst />}
              {answered && right && <Check aria-hidden className="size-7 text-green-600" />}
              {answered && chosen && !right && <X aria-hidden className="size-7 text-red-500" />}
              {pictures ? (
                <KidsPicture url={option.imageUrl} alt="" className="size-32" />
              ) : (
                <span lang="en">{option.text}</span>
              )}
            </motion.button>
          )
        })}
      </div>

      {answer.isError && (
        <div role="alert" className="flex flex-col items-center gap-2 text-center">
          <p className="text-lg">{t('kids.game.answerError')}</p>
        </div>
      )}

      {answered && (
        <div className="flex flex-col items-center gap-4 text-center" aria-live="polite">
          <motion.p
            initial={{ scale: 0.6, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 400, damping: 14 }}
            className={cn('text-2xl font-bold', answered.correct ? 'text-green-600' : 'text-foreground')}
          >
            {answered.correct ? t('kids.game.right') : t('kids.game.wrong')}
          </motion.p>
          <Button className="min-h-14 rounded-2xl px-10 text-xl" onClick={onNext}>
            {t('kids.game.next')}
          </Button>
        </div>
      )}
    </motion.div>
  )
}

/** What the question shows: the picture, the English word (which can be heard), or the sentence with its gap. */
function QuestionPrompt({ question }: { question: PlayQuestion }) {
  const { t } = useTranslation()
  switch (question.form) {
    case 'PictureToWord':
      return (
        <div className="flex flex-col items-center gap-3">
          <p className="text-xl">{t('kids.game.pickWord')}</p>
          <KidsPicture url={question.promptImageUrl} alt={t('kids.game.promptPicture')} className="size-56" />
        </div>
      )
    case 'WordToPicture':
      return (
        <div className="flex flex-col items-center gap-3">
          <p className="text-xl">{t('kids.game.pickPicture')}</p>
          <div className="flex items-center gap-3">
            <p lang="en" className="font-heading text-5xl font-bold">
              {question.promptText}
            </p>
            <ListenButton text={question.promptText ?? ''} />
          </div>
        </div>
      )
    default:
      return (
        <div className="flex flex-col items-center gap-3">
          <p className="text-xl">{t('kids.game.fillGap')}</p>
          <p lang="en" className="text-center font-heading text-4xl font-bold">
            {question.promptText}
          </p>
        </div>
      )
  }
}
