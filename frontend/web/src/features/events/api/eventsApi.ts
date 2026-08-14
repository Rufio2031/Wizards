import { httpClient, type RequestOptions } from '@/services/http/httpClient'

import type { GameEvent } from '../types/event'

// The API has no events controller yet, so this path follows the backend's
// route convention (plural, no `api` segment) rather than a verified endpoint.
const EVENTS_PATH = '/events'

/** The only place event route paths are known. */
export const eventsApi = {
  async list(options?: RequestOptions): Promise<GameEvent[]> {
    // An empty body is "no events scheduled" here, not a missing list.
    return (await httpClient.get<GameEvent[]>(EVENTS_PATH, options)) ?? []
  },
}
