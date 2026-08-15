import { computed, toValue, type MaybeRefOrGetter } from 'vue'

import { usePagedRequest } from '@/composables/usePagedRequest'
import { toLocalDay } from '@/utils/dateTime'
import { groupBy } from '@/utils/grouping'

import { eventsApi, type ListEventsParams } from '../api/eventsApi'
import type { EventSortField, GameEvent, SortDirection } from '../types/event'

const DEFAULT_PAGE_SIZE = 20

export interface UseEventsOptions {
  pageSize?: MaybeRefOrGetter<number | undefined>
  sortBy?: MaybeRefOrGetter<EventSortField | undefined>
  sortDirection?: MaybeRefOrGetter<SortDirection | undefined>

  /** Defaults to the instant `load` is called, which lists upcoming events. */
  startingOnOrAfter?: MaybeRefOrGetter<Date | undefined>
  startingBefore?: MaybeRefOrGetter<Date | undefined>
}

export interface EventDayGroup {
  key: string
  label: string
  events: GameEvent[]
}

function groupEventsByDay(events: readonly GameEvent[]): EventDayGroup[] {
  return groupBy(events, (event) => toLocalDay(event.startDateTime).key).map(
    ({ items }) => {
      const day = toLocalDay(items[0].startDateTime)

      return { key: day.key, label: day.label, events: items }
    },
  )
}

/**
 * Loads events one page at a time, grouped by calendar day, upcoming by default.
 *
 * @param options Page size, sort, and the window an event's start must fall in, each read when `load` runs.
 * @returns The accumulated events and their day groups, alongside the paging state and actions.
 */
export function useEvents(options: UseEventsOptions = {}) {
  function pinQuery(): Omit<ListEventsParams, 'skip'> {
    return {
      take: toValue(options.pageSize) ?? DEFAULT_PAGE_SIZE,
      sortBy: toValue(options.sortBy),
      sortDirection: toValue(options.sortDirection),
      startingOnOrAfter: toValue(options.startingOnOrAfter) ?? new Date(),
      startingBefore: toValue(options.startingBefore),
    }
  }

  const {
    items: events,
    pagination,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    loadFailed,
    loadMoreFailed,
    load,
    loadMore,
  } = usePagedRequest<GameEvent, Omit<ListEventsParams, 'skip'>>(
    (requestOptions, params) => eventsApi.list(params, requestOptions),
    { pinQuery, failureMessage: 'Loading events failed.' },
  )

  const eventGroups = computed(() => groupEventsByDay(events.value))

  return {
    events,
    eventGroups,
    pagination,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    loadFailed,
    loadMoreFailed,
    load,
    loadMore,
  }
}
