import { LogOut } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { signOut, useSession } from '@/shared/auth/session'
import { Button } from '@/shared/components/ui/button'

/** The signed-in user's email and the sign-out button, for the Admin and Parent headers. */
export function AccountMenu() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const session = useSession()
  const [signingOut, setSigningOut] = useState(false)

  if (!session) {
    return null
  }

  const handleSignOut = async () => {
    setSigningOut(true)
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-w-0 items-center gap-2">
      <span className="truncate text-sm text-muted-foreground">{session.user.email}</span>
      <Button variant="outline" size="sm" disabled={signingOut} onClick={() => void handleSignOut()}>
        <LogOut aria-hidden />
        {t('auth.signOut')}
      </Button>
    </div>
  )
}
