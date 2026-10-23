import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider, createBrowserRouter } from 'react-router'
import { clearCacheOnSignOut, createQueryClient } from '@/shared/api/query-client'
import { routes } from '@/app/routes'
import { restoreSession } from '@/shared/auth/session'
import '@/shared/i18n'
import '@/index.css'

const queryClient = createQueryClient()
clearCacheOnSignOut(queryClient)
const router = createBrowserRouter(routes)

// The guards show "checking your session" until this settles.
void restoreSession()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
)
