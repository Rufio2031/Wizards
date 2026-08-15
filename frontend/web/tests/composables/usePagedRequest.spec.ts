import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref, type EffectScope } from 'vue'

import { usePagedRequest } from '@/composables/usePagedRequest'
import type { RequestOptions } from '@/services/http/httpClient'
import type { Page } from '@/services/http/pagination'

const FAILURE_MESSAGE = 'Loading the wizards failed.'

const wizards = ['Merlin', 'Morgana', 'Circe', 'Gandalf', 'Baba Yaga']

interface Query {
  take: number
  tier: string
}

type PageRequest = Query & { skip: number }

interface Deferred<T> {
  promise: Promise<T>
  resolve: (value: T) => void
  reject: (reason?: unknown) => void
}

function deferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void

  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })

  return { promise, resolve, reject }
}

function pageOf(source: string[], skip: number, take: number): Page<string> {
  return {
    items: source.slice(skip, skip + take),
    pagination: { skip, take, totalCount: source.length },
  }
}

const scopes: EffectScope[] = []

/** A pager over `source`, plus the levers a test needs to steer one request. */
function createPager(source: string[], take: number) {
  const requests: PageRequest[] = []
  const tier = ref('novice')

  let nextResponse: (() => Promise<Page<string>>) | null = null

  function fetchPage(
    _options: RequestOptions,
    params: PageRequest,
  ): Promise<Page<string>> {
    requests.push({ ...params })

    const override = nextResponse

    nextResponse = null

    return override
      ? override()
      : Promise.resolve(pageOf(source, params.skip, params.take))
  }

  const scope = effectScope()

  scopes.push(scope)

  const paged = scope.run(() =>
    usePagedRequest<string, Query>(fetchPage, {
      pinQuery: () => ({ take, tier: tier.value }),
      failureMessage: FAILURE_MESSAGE,
    }),
  )!

  return {
    ...paged,
    requests,
    tier,
    respondNextWith(respond: () => Promise<Page<string>>) {
      nextResponse = respond
    },
  }
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  for (const scope of scopes.splice(0)) {
    scope.stop()
  }

  vi.restoreAllMocks()
})

describe('usePagedRequest walking', () => {
  it('appends each following page until every item is loaded, once each', async () => {
    const pager = createPager(wizards, 2)

    await pager.load()

    expect(pager.items.value).toEqual(['Merlin', 'Morgana'])

    await pager.loadMore()

    expect(pager.items.value).toEqual([
      'Merlin',
      'Morgana',
      'Circe',
      'Gandalf',
    ])

    await pager.loadMore()

    expect(pager.items.value).toEqual(wizards)
  })

  it('starts over from the first page when the list is loaded again', async () => {
    const pager = createPager(wizards, 2)

    await pager.load()
    await pager.loadMore()

    expect(pager.items.value).toHaveLength(4)

    await pager.load()

    expect(pager.items.value).toEqual(['Merlin', 'Morgana'])
    expect(pager.pagination.value).toEqual({ skip: 0, take: 2, totalCount: 5 })
  })

  it('carries one pinned query through a walk and re-reads it on the next load', async () => {
    const pager = createPager(wizards, 2)

    await pager.load()

    pager.tier.value = 'archmage'

    await pager.loadMore()
    await pager.loadMore()

    expect(pager.requests).toEqual([
      { skip: 0, take: 2, tier: 'novice' },
      { skip: 2, take: 2, tier: 'novice' },
      { skip: 4, take: 2, tier: 'novice' },
    ])

    await pager.load()

    expect(pager.requests[3]).toEqual({ skip: 0, take: 2, tier: 'archmage' })
  })

  it('reports more to come until the loaded count reaches the total', async () => {
    const pager = createPager(['Merlin', 'Morgana', 'Circe', 'Gandalf'], 2)

    await pager.load()

    expect(pager.hasMore.value).toBe(true)

    await pager.loadMore()

    expect(pager.items.value).toHaveLength(4)
    expect(pager.hasMore.value).toBe(false)
  })

  it('asks for nothing more while a page is in flight, or once nothing remains', async () => {
    const pager = createPager(wizards, 2)
    const pending = deferred<Page<string>>()

    await pager.load()

    pager.respondNextWith(() => pending.promise)

    const first = pager.loadMore()
    const second = pager.loadMore()

    pending.resolve(pageOf(wizards, 2, 2))

    await Promise.all([first, second])

    expect(pager.items.value).toHaveLength(4)

    await pager.loadMore()
    await pager.loadMore()

    expect(pager.items.value).toEqual(wizards)
    expect(pager.requests.map((request) => request.skip)).toEqual([0, 2, 4])
  })
})

describe('usePagedRequest state', () => {
  it('reports loading for the first page and loading more for the pages after it', async () => {
    const pager = createPager(wizards, 2)
    const firstPage = deferred<Page<string>>()

    pager.respondNextWith(() => firstPage.promise)

    const loading = pager.load()

    expect(pager.isLoading.value).toBe(true)
    expect(pager.isLoadingMore.value).toBe(false)

    firstPage.resolve(pageOf(wizards, 0, 2))
    await loading

    const secondPage = deferred<Page<string>>()

    pager.respondNextWith(() => secondPage.promise)

    const loadingMore = pager.loadMore()

    expect(pager.isLoading.value).toBe(false)
    expect(pager.isLoadingMore.value).toBe(true)

    secondPage.resolve(pageOf(wizards, 2, 2))
    await loadingMore

    expect(pager.isLoading.value).toBe(false)
    expect(pager.isLoadingMore.value).toBe(false)
  })

  it('keeps the loaded items and reports only a load-more failure when a later page fails', async () => {
    const pager = createPager(wizards, 2)

    await pager.load()

    pager.respondNextWith(() =>
      Promise.reject(new Error('The tower is unreachable.')),
    )

    await pager.loadMore()

    expect(pager.items.value).toEqual(['Merlin', 'Morgana'])
    expect(pager.error.value?.message).toBe('The tower is unreachable.')
    expect(pager.loadMoreFailed.value).toBe(true)
    expect(pager.loadFailed.value).toBe(false)
    expect(pager.hasMore.value).toBe(true)
  })

  it('reports only a load failure when the first page fails, and clears it on a retry', async () => {
    const pager = createPager(wizards, 2)

    pager.respondNextWith(() =>
      Promise.reject(new Error('The tower is unreachable.')),
    )

    await pager.load()

    expect(pager.items.value).toEqual([])
    expect(pager.loadFailed.value).toBe(true)
    expect(pager.loadMoreFailed.value).toBe(false)

    await pager.load()

    expect(pager.error.value).toBeNull()
    expect(pager.items.value).toEqual(['Merlin', 'Morgana'])
  })
})
