import { env } from '@/config/env'
import { httpClient, type RequestOptions } from '@/services/http/httpClient'
import type { Page } from '@/services/http/pagination'

import type {
  CreateEventRequest,
  CreateRegistrationRequest,
  EventSortField,
  GameEvent,
  Registration,
  SortDirection,
} from '../types/event'

const EVENTS_PATH = '/events'

export interface ListEventsParams {
  skip: number
  take: number
  sortBy?: EventSortField
  sortDirection?: SortDirection
  startingOnOrAfter?: Date
  startingBefore?: Date
}

/** The only place event route paths are known. */
export const eventsApi = {
  async list(
    {
      skip,
      take,
      sortBy,
      sortDirection,
      startingOnOrAfter,
      startingBefore,
    }: ListEventsParams,
    options?: RequestOptions,
  ): Promise<Page<GameEvent>> {
    const query = new URLSearchParams({
      skip: String(skip),
      take: String(take),
    })

    if (sortBy) {
      query.set('sortBy', sortBy)
    }

    if (sortDirection) {
      query.set('sortDirection', sortDirection)
    }

    if (startingOnOrAfter) {
      query.set('startingOnOrAfter', startingOnOrAfter.toISOString())
    }

    if (startingBefore) {
      query.set('startingBefore', startingBefore.toISOString())
    }

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

  // A URL for the browser to navigate to, not a request. The browser fetches it
  // and saves the response itself, so it carries the base path `httpClient`
  // would otherwise prepend.
  calendarInviteUrl(eventId: string): string {
    return `${env.apiBasePath}${EVENTS_PATH}/${encodeURIComponent(eventId)}/calendar.ics`
  },

  async create(
    request: CreateEventRequest,
    options?: RequestOptions,
  ): Promise<GameEvent> {
    const event = await httpClient.post<GameEvent>(
      EVENTS_PATH,
      request,
      options,
    )

    if (!event) {
      throw new Error('Creating an event returned no body.')
    }

    return event
  },

  async listRegistrations(
    eventId: string,
    options?: RequestOptions,
  ): Promise<Registration[]> {
    const registrations = await httpClient.get<Registration[]>(
      `${EVENTS_PATH}/${encodeURIComponent(eventId)}/registrations`,
      options,
    )

    if (!registrations) {
      throw new Error('The registrations endpoint returned no body.')
    }

    return registrations
  },

  // Resolving is the whole answer, so an empty body is the expected success and
  // whatever the endpoint returns is ignored until there is a registration to
  // address.
  async register(
    eventId: string,
    request: CreateRegistrationRequest,
    options?: RequestOptions,
  ): Promise<void> {
    await httpClient.post(
      `${EVENTS_PATH}/${encodeURIComponent(eventId)}/registrations`,
      request,
      options,
    )
  },
}
