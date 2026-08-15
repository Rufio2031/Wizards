import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import { useEvents } from '@/features/events/composables/useEvents'
import type { GameEvent, SortDirection } from '@/features/events/types/event'
import { ApiError } from '@/services/http/ApiError'
import type { Page } from '@/services/http/pagination'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, list: vi.fn() } }
})

function eventAt(
  eventId: string,
  name: string,
  startDateTime: string,
): GameEvent {
  return {
    eventId,
    name,
    location: 'The Tower',
    startDateTime,
    endDateTime: startDateTime,
    registrationLimit: 12,
    gameType: { gameTypeId: 'gt-1', name: 'Ritual' },
    selections: {},
  }
}

const morningOf14 = eventAt('evt-1', 'Summoning 101', '2026-03-14T09:30:00')
const eveningOf14 = eventAt('evt-2', 'Alchemy Night', '2026-03-14T19:00:00')
const morningOf15 = eventAt('evt-3', 'Rune Reading', '2026-03-15T09:30:00')
const eveningOf15 = eventAt('evt-4', 'Potion Lab', '2026-03-15T18:00:00')

function pageAt(
  events: GameEvent[],
  skip: number,
  take: number,
  totalCount: number,
): Page<GameEvent> {
  return { items: events, pagination: { skip, take, totalCount } }
}

function namesOf(events: readonly GameEvent[]): string[] {
  return events.map((event) => event.name)
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void

  const promise = new Promise<T>((resolveIt, rejectIt) => {
    resolve = resolveIt
    reject = rejectIt
  })

  return { promise, resolve, reject }
}

const scopes: EffectScope[] = []

function runInScope<T>(factory: () => T): T {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(factory)!
}

function respondWith(events: GameEvent[]) {
  vi.mocked(eventsApi.list).mockImplementation(async ({ skip, take }) => ({
    items: events.slice(skip, skip + take),
    pagination: { skip, take, totalCount: events.length },
  }))
}

/** Holds only the next request open, leaving any earlier setup in place. */
function holdNextResponse() {
  const pending = deferred<Page<GameEvent>>()

  vi.mocked(eventsApi.list).mockReturnValueOnce(pending.promise)

  return pending
}

/** The time bound each request carried, in the order the requests were made. */
function requestedTimeBounds(): (string | undefined)[] {
  return vi
    .mocked(eventsApi.list)
    .mock.calls.map(([params]) => params.startingOnOrAfter?.toISOString())
}

/** The query each request carried, in the order the requests were made. */
function requestedQueries() {
  return vi.mocked(eventsApi.list).mock.calls.map(([params]) => ({
    skip: params.skip,
    take: params.take,
    sortDirection: params.sortDirection,
    startingOnOrAfter: params.startingOnOrAfter?.toISOString(),
    startingBefore: params.startingBefore?.toISOString(),
  }))
}

beforeEach(() => {
  vi.resetAllMocks()
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  while (scopes.length) {
    scopes.pop()!.stop()
  }

  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('useEvents', () => {
  it('loads nothing until it is asked to', () => {
    const { events, eventGroups } = runInScope(() => useEvents())

    expect(events.value).toEqual([])
    expect(eventGroups.value).toEqual([])
    expect(eventsApi.list).not.toHaveBeenCalled()
  })

  it('exposes one page of events, in the order the API returned them', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const { events, pagination, load } = runInScope(() =>
      useEvents({ pageSize: 2 }),
    )

    await load()

    expect(namesOf(events.value)).toEqual(['Summoning 101', 'Alchemy Night'])
    expect(pagination.value).toEqual({ skip: 0, take: 2, totalCount: 3 })
  })

  it('collects the events of one calendar day into a single group', async () => {
    respondWith([morningOf14, morningOf15, eveningOf14])

    const { eventGroups, load } = runInScope(() => useEvents())

    await load()

    expect(eventGroups.value.map((group) => group.key)).toEqual([
      '2026-03-14',
      '2026-03-15',
    ])
    expect(namesOf(eventGroups.value[0].events)).toEqual([
      'Summoning 101',
      'Alchemy Night',
    ])
    expect(namesOf(eventGroups.value[1].events)).toEqual(['Rune Reading'])
  })

  it('gathers events with no usable start under an announced-later group', async () => {
    respondWith([eventAt('evt-9', 'Mystery Moot', '')])

    const { eventGroups, load } = runInScope(() => useEvents())

    await load()

    expect(eventGroups.value).toHaveLength(1)
    expect(eventGroups.value[0].key).toBe('unknown-day')
    expect(eventGroups.value[0].label).toBe('Date to be announced')
  })

  it('reports the failure and leaves the list empty when loading fails', async () => {
    vi.mocked(eventsApi.list).mockRejectedValue(
      new ApiError(500, { detail: 'The registry is unavailable.' }),
    )

    const {
      events,
      eventGroups,
      error,
      isLoading,
      loadFailed,
      loadMoreFailed,
      load,
    } = runInScope(() => useEvents())

    await load()

    expect(error.value?.message).toBe('The registry is unavailable.')
    expect(events.value).toEqual([])
    expect(eventGroups.value).toEqual([])
    expect(isLoading.value).toBe(false)
    expect(loadFailed.value).toBe(true)
    expect(loadMoreFailed.value).toBe(false)
  })

  it('drops the previous failure when a retry succeeds', async () => {
    vi.mocked(eventsApi.list).mockRejectedValueOnce(
      new ApiError(503, { detail: 'The registry is unavailable.' }),
    )

    const { events, error, load } = runInScope(() => useEvents())

    await load()

    expect(error.value).not.toBeNull()

    respondWith([morningOf14])
    await load()

    expect(error.value).toBeNull()
    expect(namesOf(events.value)).toEqual(['Summoning 101'])
  })

  it('walks the list one page at a time without repeating or skipping an event', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const { events, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 1 }),
    )

    await load()

    expect(namesOf(events.value)).toEqual(['Summoning 101'])

    await loadMore()

    expect(namesOf(events.value)).toEqual(['Summoning 101', 'Alchemy Night'])

    await loadMore()

    expect(namesOf(events.value)).toEqual([
      'Summoning 101',
      'Alchemy Night',
      'Rune Reading',
    ])
  })

  it('reports nothing more to come when the last page fills exactly', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15, eveningOf15])

    const { events, hasMore, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 2 }),
    )

    await load()
    await loadMore()

    expect(events.value).toHaveLength(4)
    expect(hasMore.value).toBe(false)

    await loadMore()

    expect(eventsApi.list).toHaveBeenCalledTimes(2)
  })

  it('asks for nothing more before the first page has loaded', async () => {
    respondWith([morningOf14, eveningOf14])

    const { events, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 1 }),
    )

    await loadMore()

    expect(events.value).toEqual([])
    expect(eventsApi.list).not.toHaveBeenCalled()

    await load()

    expect(namesOf(events.value)).toEqual(['Summoning 101'])
  })

  it('ignores a request for more while a page is still in flight', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const { events, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 1 }),
    )

    await load()

    const pending = holdNextResponse()
    const first = loadMore()
    const second = loadMore()

    pending.resolve(pageAt([eveningOf14], 1, 1, 3))
    await Promise.all([first, second])

    expect(namesOf(events.value)).toEqual(['Summoning 101', 'Alchemy Night'])
    expect(eventsApi.list).toHaveBeenCalledTimes(2)
  })

  it('reports loading more, and not loading, while a later page is in flight', async () => {
    respondWith([morningOf14, eveningOf14])

    const { isLoading, isLoadingMore, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 1 }),
    )

    await load()

    const pending = holdNextResponse()
    const loadingMore = loadMore()

    expect(isLoadingMore.value).toBe(true)
    expect(isLoading.value).toBe(false)

    pending.resolve(pageAt([eveningOf14], 1, 1, 2))
    await loadingMore

    expect(isLoadingMore.value).toBe(false)
    expect(isLoading.value).toBe(false)
  })

  it('reports loading, and not loading more, while the first page is in flight', async () => {
    respondWith([morningOf14, eveningOf14])

    const { isLoading, isLoadingMore, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 1 }),
    )

    await load()
    await loadMore()

    const pending = holdNextResponse()
    const reloading = load()

    expect(isLoading.value).toBe(true)
    expect(isLoadingMore.value).toBe(false)

    pending.resolve(pageAt([morningOf14], 0, 1, 2))
    await reloading

    expect(isLoading.value).toBe(false)
    expect(isLoadingMore.value).toBe(false)
  })

  it('keeps the loaded events when loading more fails', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const {
      events,
      eventGroups,
      hasMore,
      error,
      loadFailed,
      loadMoreFailed,
      load,
      loadMore,
    } = runInScope(() => useEvents({ pageSize: 2 }))

    await load()

    vi.mocked(eventsApi.list).mockRejectedValueOnce(
      new ApiError(500, { detail: 'The registry is unavailable.' }),
    )

    await loadMore()

    expect(loadMoreFailed.value).toBe(true)
    expect(loadFailed.value).toBe(false)
    expect(error.value?.message).toBe('The registry is unavailable.')
    expect(namesOf(events.value)).toEqual(['Summoning 101', 'Alchemy Night'])
    expect(eventGroups.value).toHaveLength(1)
    expect(hasMore.value).toBe(true)
  })

  it('collapses back to a single page when the list is loaded again', async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const { events, hasMore, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 2 }),
    )

    await load()
    await loadMore()

    expect(events.value).toHaveLength(3)

    await load()

    expect(namesOf(events.value)).toEqual(['Summoning 101', 'Alchemy Night'])
    expect(hasMore.value).toBe(true)
  })

  it('adds an appended event to the day group it already opened', async () => {
    respondWith([morningOf14, morningOf15, eveningOf14])

    const { eventGroups, load, loadMore } = runInScope(() =>
      useEvents({ pageSize: 2 }),
    )

    await load()
    await loadMore()

    expect(eventGroups.value.map((group) => group.key)).toEqual([
      '2026-03-14',
      '2026-03-15',
    ])
    expect(namesOf(eventGroups.value[0].events)).toEqual([
      'Summoning 101',
      'Alchemy Night',
    ])
    expect(namesOf(eventGroups.value[1].events)).toEqual(['Rune Reading'])
  })

  it('holds one time bound across a paging walk and takes a fresh one on reload', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-01T08:00:00Z'))
    respondWith([morningOf14, eveningOf14, morningOf15])

    const { load, loadMore } = runInScope(() => useEvents({ pageSize: 1 }))

    await load()
    await loadMore()

    expect(requestedTimeBounds()).toEqual([
      '2026-03-01T08:00:00.000Z',
      '2026-03-01T08:00:00.000Z',
    ])

    vi.setSystemTime(new Date('2026-03-01T08:05:00Z'))

    await load()

    expect(requestedTimeBounds()[2]).toBe('2026-03-01T08:05:00.000Z')
  })

  it("holds the caller's query across a paging walk and re-reads it on reload", async () => {
    respondWith([morningOf14, eveningOf14, morningOf15])

    const sortDirection = ref<SortDirection>('Descending')

    const { load, loadMore } = runInScope(() =>
      useEvents({
        pageSize: 1,
        sortDirection,
        startingOnOrAfter: new Date('2026-03-10T00:00:00Z'),
        startingBefore: new Date('2026-03-20T00:00:00Z'),
      }),
    )

    await load()

    sortDirection.value = 'Ascending'

    await loadMore()

    expect(requestedQueries()).toEqual([
      {
        skip: 0,
        take: 1,
        sortDirection: 'Descending',
        startingOnOrAfter: '2026-03-10T00:00:00.000Z',
        startingBefore: '2026-03-20T00:00:00.000Z',
      },
      {
        skip: 1,
        take: 1,
        sortDirection: 'Descending',
        startingOnOrAfter: '2026-03-10T00:00:00.000Z',
        startingBefore: '2026-03-20T00:00:00.000Z',
      },
    ])

    await load()

    expect(requestedQueries()[2].sortDirection).toBe('Ascending')
  })
})
