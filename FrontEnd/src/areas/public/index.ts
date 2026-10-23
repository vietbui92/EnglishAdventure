// The public pages that load on demand (they bring the form libraries): one chunk, see routes.tsx.
// The landing page is not here: it stays in the main bundle.
export { CheckEmailPage } from '@/areas/public/CheckEmailPage'
export { ConfirmEmailPage } from '@/areas/public/ConfirmEmailPage'
export { PrivacyPage, TermsPage } from '@/areas/public/LegalPage'
export { LoginPage } from '@/areas/public/LoginPage'
export { RegisterPage } from '@/areas/public/RegisterPage'
