import { useQuery } from '@tanstack/react-query'
import { api, unwrap } from '@/api/client'

/** Whether the API answers through the dev proxy. Used by the temporary home page. */
export function useApiStatus() {
  return useQuery({
    queryKey: ['foundation', 'status'],
    queryFn: ({ signal }) => unwrap(api.GET('/api/v1/foundation/status', { signal })),
  })
}
