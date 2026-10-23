import { describe, expect, it } from 'vitest'
import { ApiError, kindForStatus, toApiError } from '@/shared/api/problem-details'

describe('kindForStatus', () => {
  it.each([
    [400, 'validation'],
    [401, 'unauthorized'],
    [403, 'forbidden'],
    [404, 'not-found'],
    [409, 'conflict'],
    [422, 'rule'],
    [500, 'server'],
    [502, 'server'],
  ] as const)('maps %i to %s', (status, kind) => {
    expect(kindForStatus(status)).toBe(kind)
  })
})

describe('toApiError', () => {
  it('uses the problem detail as the message', () => {
    const error = toApiError(422, { title: 'Domain rule violation.', status: 422, detail: 'The last SuperAdmin.' })

    expect(error).toBeInstanceOf(ApiError)
    expect(error.kind).toBe('rule')
    expect(error.message).toBe('The last SuperAdmin.')
  })

  it('maps validation errors to camelCase field names', () => {
    const error = toApiError(400, {
      title: 'Validation failed.',
      status: 400,
      errors: { Email: ['Email is required.'], '$.password': ['Invalid JSON.'], Password: ['Too short.'] },
    })

    expect(error.fieldErrors).toEqual({
      email: ['Email is required.'],
      password: ['Invalid JSON.', 'Too short.'],
    })
  })

  it('keeps the status when the body is not a problem', () => {
    const error = toApiError(502, 'Bad Gateway')

    expect(error.kind).toBe('server')
    expect(error.status).toBe(502)
    expect(error.problem).toBeNull()
    expect(error.fieldErrors).toEqual({})
  })
})
