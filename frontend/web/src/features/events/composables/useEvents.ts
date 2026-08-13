import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { eventsApi } from '../api/eventsApi'
import type { GameEvent } from '../types/event'

/**
 * Loads the scheduled event list and tracks its request state.
 *
 * @returns `events` list, `isLoading` while a request is in flight, `error`
 * from the last failure (a log detail, not display copy), and `refresh` to
 * reload. Reloading and scope disposal both abort the in-flight request.
 */
export function useEvents() {
  const {
    data: events,
    isLoading,
    error,
    refresh,
  } = useAsyncRequest<GameEvent[]>((options) => eventsApi.list(options), {
    initialValue: [],
    failureMessage: 'Loading events failed.',
  })

  return { events, isLoading, error, refresh }
}
