import { computed, shallowRef, toValue, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import { emptyPage, type Page } from '@/services/http/pagination'
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

/** Everything but the offset, held still for the length of one paging walk. */
type PinnedQuery = Omit<ListEventsParams, 'skip'>

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
 * Loads events one page at a time, grouped by calendar day, upcoming by
 * default.
 *
 * Pages accumulate, so the day groups only ever grow until `load` starts over.
 * Options are read when `load` runs rather than watched, so a caller that
 * changes one calls `load` to re-query from the first page.
 *
 * @param options How the events are queried: page size, sort, and the window
 * their start must fall in.
 * @returns The accumulated events and their day groups, the states each region
 * of the list renders from, and the `load` and `loadMore` actions.
 */
export function useEvents(options: UseEventsOptions = {}) {
  const loaded = shallowRef<GameEvent[]>([])
  const requestedSkip = shallowRef(0)

  function resolvePageSize(): number {
    return toValue(options.pageSize) ?? DEFAULT_PAGE_SIZE
  }

  // One set of query values for the whole offset walk. A bound taken per
  // request would move as events start, shifting the window out from under the
  // skip and either dropping an event or appending one already loaded.
  function pinQuery(): PinnedQuery {
    return {
      take: resolvePageSize(),
      sortBy: toValue(options.sortBy),
      sortDirection: toValue(options.sortDirection),
      startingOnOrAfter: toValue(options.startingOnOrAfter) ?? new Date(),
      startingBefore: toValue(options.startingBefore),
    }
  }

  let query = pinQuery()

  const {
    data,
    isLoading: isFetching,
    error,
    run,
  } = useAsyncRequest<Page<GameEvent>, ListEventsParams>(
    (requestOptions, params) => eventsApi.list(params, requestOptions),
    {
      initialValue: emptyPage<GameEvent>(resolvePageSize()),
      failureMessage: 'Loading events failed.',
    },
  )

  const events = computed(() => loaded.value)
  const eventGroups = computed(() => groupEventsByDay(events.value))
  const pagination = computed(() => data.value.pagination)

  const hasMore = computed(
    () => loaded.value.length < pagination.value.totalCount,
  )

  const isFirstPage = computed(() => requestedSkip.value === 0)
  const isLoading = computed(() => isFetching.value && isFirstPage.value)
  const isLoadingMore = computed(() => isFetching.value && !isFirstPage.value)
  const loadFailed = computed(() => !!error.value && isFirstPage.value)
  const loadMoreFailed = computed(() => !!error.value && !isFirstPage.value)

  async function fetchFrom(skip: number): Promise<void> {
    requestedSkip.value = skip

    const page = await run({ skip, ...query })

    if (!page) {
      return
    }

    loaded.value = skip === 0 ? page.items : [...loaded.value, ...page.items]
  }

  /** Starts the list over at the first page, against freshly read options. */
  function load(): Promise<void> {
    query = pinQuery()

    return fetchFrom(0)
  }

  /** Appends the page that follows what is already loaded. */
  function loadMore(): Promise<void> {
    if (isFetching.value || !hasMore.value) {
      return Promise.resolve()
    }

    return fetchFrom(loaded.value.length)
  }

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
