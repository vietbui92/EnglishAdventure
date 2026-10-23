import { waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '@/api/problem-details'
import { useApiStatus } from '@/app/pages/useApiStatus'
import { mockApi, problem } from '@/test/api-mocks'
import { renderHookWithProviders } from '@/test/render'
import { server } from '@/test/server'

describe('useApiStatus', () => {
  it('returns the foundation status', async () => {
    server.use(
      mockApi('get', '/api/v1/foundation/status', () => ({
        message: 'English Adventure API foundation is ready.',
        isFoundationExample: true,
        echo: null,
      })),
    )

    const { result } = renderHookWithProviders(() => useApiStatus())

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.message).toBe('English Adventure API foundation is ready.')
  })

  it('fails with an ApiError for a server error', async () => {
    server.use(mockApi('get', '/api/v1/foundation/status', () => problem(503)))

    const { result } = renderHookWithProviders(() => useApiStatus())

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect((result.current.error as ApiError).kind).toBe('server')
  })
})
