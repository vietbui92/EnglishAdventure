import type { ParseKeys } from 'i18next'
import { CircleAlert, Pencil } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { isHttpsUrl } from '@/areas/admin/content/WordDialog'
import { useBadgeDefinitions, useUpdateBadge, type BadgeDefinition } from '@/areas/admin/rewards/api'
import { Alert, AlertDescription } from '@/shared/components/ui/alert'
import { BadgeMedal } from '@/shared/components/BadgeMedal'
import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { Textarea } from '@/shared/components/ui/textarea'
import { zodResolver } from '@/shared/lib/zod-resolver'

// Same limits as the backend BadgeDefinition entity.
const nameMaxLength = 60
const descriptionMaxLength = 200

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'admin.rewards.form.validation.nameRequired' satisfies ParseKeys)
    .max(nameMaxLength, 'admin.rewards.form.validation.nameTooLong' satisfies ParseKeys),
  description: z
    .string()
    .trim()
    .min(1, 'admin.rewards.form.validation.descriptionRequired' satisfies ParseKeys)
    .max(descriptionMaxLength, 'admin.rewards.form.validation.descriptionTooLong' satisfies ParseKeys),
  imageUrl: z
    .string()
    .trim()
    .refine((value) => value === '' || isHttpsUrl(value), {
      message: 'admin.content.words.form.validation.imageUrlHttps' satisfies ParseKeys,
    }),
})

/**
 * The badges (F2.6, S5.2; owner 2026-10-24): the four badges of PD-011 with their names, descriptions and pictures,
 * which a SuperAdmin can change. The rules that award them are fixed. A level badge's texts can say {level}, which
 * children see as the island's name.
 */
export function BadgesPage() {
  const { t } = useTranslation()
  const badges = useBadgeDefinitions()
  const [editing, setEditing] = useState<BadgeDefinition | null>(null)

  return (
    <div className="flex max-w-4xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">{t('admin.rewards.title')}</h1>
        <p className="text-muted-foreground">{t('admin.rewards.description')}</p>
      </div>

      {badges.isPending ? (
        <p role="status" className="text-muted-foreground">
          {t('admin.rewards.loading')}
        </p>
      ) : badges.isError ? (
        <Alert variant="destructive">
          <CircleAlert aria-hidden />
          <AlertDescription>{t('admin.rewards.loadError')}</AlertDescription>
        </Alert>
      ) : (
        <ul className="grid gap-4 sm:grid-cols-2">
          {badges.data.map((badge) => (
            <li key={badge.kind} className="flex gap-4 rounded-xl border bg-card p-4">
              <BadgeMedal kind={badge.kind} imageUrl={badge.imageUrl} />
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <p className="text-sm text-muted-foreground">
                  {t(`admin.rewards.kinds.${badge.kind}` as ParseKeys)}
                  {badge.perLevel && ` · ${t('admin.rewards.perLevel')}`}
                </p>
                <h2 className="font-semibold break-words">{badge.name}</h2>
                <p className="text-sm break-words">{badge.description}</p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-2 self-start"
                  aria-label={t('admin.rewards.editBadge', { name: badge.name })}
                  onClick={() => setEditing(badge)}
                >
                  <Pencil aria-hidden />
                  {t('admin.rewards.edit')}
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <Dialog open={editing !== null} onOpenChange={(open) => !open && setEditing(null)}>
        <DialogContent showCloseButton={false}>
          {editing && <BadgeForm badge={editing} onClose={() => setEditing(null)} />}
        </DialogContent>
      </Dialog>
    </div>
  )
}

function BadgeForm({ badge, onClose }: { badge: BadgeDefinition; onClose: () => void }) {
  const { t } = useTranslation()
  const update = useUpdateBadge()
  const form = useForm<z.input<typeof schema>, unknown, z.output<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: { name: badge.name, description: badge.description, imageUrl: badge.imageUrl ?? '' },
  })
  const { errors, isSubmitting } = form.formState
  const imageUrl = useWatch({ control: form.control, name: 'imageUrl' }).trim()

  const submit = form.handleSubmit(async (values) => {
    try {
      await update.mutateAsync({ kind: badge.kind, ...values })
      onClose()
    } catch {
      // The alert below shows the failure; the dialog stays open.
    }
  })

  const message = (name: 'name' | 'description' | 'imageUrl') => {
    const key = errors[name]?.message
    return key ? (
      <p id={`badge-${name}-error`} className="text-sm text-destructive">
        {t(key as ParseKeys)}
      </p>
    ) : null
  }

  return (
    <form noValidate onSubmit={(event) => void submit(event)} className="flex flex-col gap-5">
      <DialogHeader>
        <DialogTitle>
          {t('admin.rewards.form.title', { kind: t(`admin.rewards.kinds.${badge.kind}` as ParseKeys) })}
        </DialogTitle>
        <DialogDescription>
          {badge.perLevel ? t('admin.rewards.form.levelHint') : t('admin.rewards.form.hint')}
        </DialogDescription>
      </DialogHeader>

      {update.isError && (
        <Alert variant="destructive">
          <CircleAlert aria-hidden />
          <AlertDescription>{t('admin.rewards.saveError')}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="badge-name">{t('admin.rewards.form.name')}</Label>
        <Input
          id="badge-name"
          maxLength={nameMaxLength}
          aria-invalid={errors.name ? true : undefined}
          aria-describedby={errors.name ? 'badge-name-error' : undefined}
          {...form.register('name')}
        />
        {message('name')}
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="badge-description">{t('admin.rewards.form.descriptionLabel')}</Label>
        <Textarea
          id="badge-description"
          maxLength={descriptionMaxLength}
          aria-invalid={errors.description ? true : undefined}
          aria-describedby={errors.description ? 'badge-description-error' : undefined}
          {...form.register('description')}
        />
        {message('description')}
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="badge-imageUrl">{t('admin.rewards.form.imageUrl')}</Label>
        <div className="flex items-start gap-3">
          <Input
            id="badge-imageUrl"
            type="url"
            inputMode="url"
            placeholder="https://"
            aria-invalid={errors.imageUrl ? true : undefined}
            aria-describedby={errors.imageUrl ? 'badge-imageUrl-error' : 'badge-imageUrl-hint'}
            {...form.register('imageUrl')}
          />
          <BadgeMedal kind={badge.kind} imageUrl={isHttpsUrl(imageUrl) ? imageUrl : null} size="sm" />
        </div>
        {message('imageUrl') ?? (
          <p id="badge-imageUrl-hint" className="text-sm text-muted-foreground">
            {t('admin.rewards.form.imageUrlHint')}
          </p>
        )}
      </div>

      <DialogFooter>
        <Button type="button" variant="ghost" onClick={onClose}>
          {t('admin.content.actions.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('admin.content.form.saving') : t('admin.content.form.save')}
        </Button>
      </DialogFooter>
    </form>
  )
}
