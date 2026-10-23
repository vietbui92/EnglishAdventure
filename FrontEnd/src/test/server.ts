import { setupServer } from 'msw/node'

/** Mock API for tests. No default handlers: each test declares the endpoints it uses with `server.use(...)`. */
export const server = setupServer()
