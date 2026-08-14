import { httpClient, type RequestOptions } from '@/services/http/httpClient'
import type { Page } from '@/services/http/pagination'

import type { GameEvent } from '../types/event'

const EVENTS_PATH = '/events'

export interface ListEventsParams {
  skip: number
  take: number
}

/** The only place event route paths are known. */
export const eventsApi = {
  async list(
    { skip, take }: ListEventsParams,
    options?: RequestOptions,
  ): Promise<Page<GameEvent>> {
    const query = new URLSearchParams({
      skip: String(skip),
      take: String(take),
    })

    const page = await httpClient.get<Page<GameEvent>>(
      `${EVENTS_PATH}?${query}`,
      options,
    )

    if (!page) {
      throw new Error('The events endpoint returned no body.')
    }

    return page
  },

  async getById(eventId: string, options?: RequestOptions): Promise<GameEvent> {
    const event = await httpClient.get<GameEvent>(
      `${EVENTS_PATH}/${encodeURIComponent(eventId)}`,
      options,
    )

    if (!event) {
      throw new Error('The event endpoint returned no body.')
    }

    return event
  },
}
