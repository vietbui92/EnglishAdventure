/** RFC 9457 problem body, as written by the backend's GlobalExceptionHandler and model validation. */
export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | string | null
  detail?: string | null
  instance?: string | null
  errors?: Record<string, string[]>
}

/**
 * What the UI does with a failed request. Mirrors the backend's status mapping:
 * 400 validation, 401 not signed in, 403 no permission, 404 not found, 409 conflict, 422 business rule.
 */
export type ApiErrorKind =
  'validation' | 'unauthorized' | 'forbidden' | 'not-found' | 'conflict' | 'rule' | 'server' | 'network'

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  /** HTTP status, or 0 when the request never got a response. */
  readonly status: number
  readonly problem: ProblemDetails | null
  /** Validation messages keyed by camelCase field name, ready for form fields. */
  readonly fieldErrors: Record<string, string[]>

  constructor(kind: ApiErrorKind, status: number, problem: ProblemDetails | null, options?: ErrorOptions) {
    super(problem?.detail ?? problem?.title ?? `Request failed (${kind})`, options)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.problem = problem
    this.fieldErrors = normalizeFieldErrors(problem?.errors)
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'status' in value || 'errors' in value)
}

export function kindForStatus(status: number): ApiErrorKind {
  switch (status) {
    case 400:
      return 'validation'
    case 401:
      return 'unauthorized'
    case 403:
      return 'forbidden'
    case 404:
      return 'not-found'
    case 409:
      return 'conflict'
    case 422:
      return 'rule'
    default:
      return 'server'
  }
}

export function toApiError(status: number, body: unknown): ApiError {
  return new ApiError(kindForStatus(status), status, isProblemDetails(body) ? body : null)
}

// FluentValidation uses property names ("Email"), JSON binding uses paths ("$.email"); forms use "email".
function normalizeFieldErrors(errors: Record<string, string[]> | undefined): Record<string, string[]> {
  const result: Record<string, string[]> = {}
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const name = key.replace(/^\$\.?/, '')
    const field = name.charAt(0).toLowerCase() + name.slice(1)
    result[field] = [...(result[field] ?? []), ...messages]
  }
  return result
}
