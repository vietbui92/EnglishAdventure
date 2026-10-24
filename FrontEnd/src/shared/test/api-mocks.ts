import { http, HttpResponse, type JsonBodyType } from 'msw'
import type { ProblemDetails } from '@/shared/api/problem-details'
import type { paths } from '@/shared/api/schema'

type Method = 'get' | 'post' | 'put' | 'delete'

/** API paths that have the given method. */
type PathsWith<M extends Method> = {
  [P in keyof paths]: [NonNullable<paths[P][M]>] extends [never] ? never : P
}[keyof paths]

type JsonBody<Response> = Response extends { content: { 'application/json': infer Body } } ? Body : undefined

/** The JSON body of the operation's 200 or 201 response; `undefined` for 204 No Content. */
type SuccessBody<Operation> = Operation extends { responses: infer Responses }
  ? 200 extends keyof Responses
    ? JsonBody<Responses[200]>
    : 201 extends keyof Responses
      ? JsonBody<Responses[201]>
      : undefined
  : never

type Resolver<Body> = (info: {
  request: Request
  params: Record<string, string | readonly string[] | undefined>
}) => Body | Response | Promise<Body | Response>

/**
 * An MSW handler whose path and success body are checked against the generated API schema, so mocks break when the
 * backend contract changes. Return a body for success, or a `Response` (see {@link problem}) for anything else.
 */
export function mockApi<M extends Method, P extends PathsWith<M>>(
  method: M,
  path: P,
  resolve: Resolver<SuccessBody<NonNullable<paths[P][M]>>>,
) {
  // OpenAPI writes path parameters as {userId}; MSW expects :userId.
  const mswPath = path.replace(/\{(\w+)\}/g, ':$1')

  return http[method](mswPath, async ({ request, params }) => {
    const result = await resolve({ request, params })
    if (result instanceof Response) {
      return result
    }
    return result === undefined ? new HttpResponse(null, { status: 204 }) : HttpResponse.json(result as JsonBodyType)
  })
}

/** A ProblemDetails error response, shaped like the backend's GlobalExceptionHandler output. */
export function problem(status: number, details: Omit<ProblemDetails, 'status'> = {}) {
  return HttpResponse.json({ title: `HTTP ${status}`, ...details, status }, { status })
}

/** For tests that only pass through the kids area's map: an adventure without published levels. */
export function mockEmptyAdventure() {
  return [mockApi('get', '/api/v1/play/map', () => ({ currentLevel: 0, points: 0, levels: [] }))]
}

/** For tests that only pass through the Parent area: a family without child profiles. */
export function mockFamilyWithoutChildren() {
  return [
    mockApi('get', '/api/v1/family/children', () => []),
    mockApi('get', '/api/v1/family/children/options', () => ({
      avatarKeys: [
        'fox',
        'owl',
        'cat',
        'bear',
        'rabbit',
        'panda',
        'lion',
        'penguin',
        'elephant',
        'monkey',
        'koala',
        'turtle',
      ],
      maxActive: 3,
    })),
  ]
}
