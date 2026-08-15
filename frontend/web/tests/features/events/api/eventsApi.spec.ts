import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { eventsApi } from '@/features/events/api/eventsApi'

const originalFetch = globalThis.fetch

let fetchMock: ReturnType<typeof vi.fn>

function pageResponse(): Response {
  return new Response(
    JSON.stringify({
      items: [],
      pagination: { skip: 0, take: 20, totalCount: 0 },
    }),
    { status: 200, headers: { 'content-type': 'application/json' } },
  )
}

function lastUrl(): string {
  const call = fetchMock.mock.calls.at(-1)

  expect(call).toBeDefined()

  return call![0] as string
}

beforeEach(() => {
  fetchMock = vi.fn()
  globalThis.fetch = fetchMock as unknown as typeof fetch
})

afterEach(() => {
  globalThis.fetch = originalFetch
  vi.restoreAllMocks()
})

describe('eventsApi.list', () => {
  it('asks for events starting on or after the given instant, in UTC', async () => {
    fetchMock.mockResolvedValue(pageResponse())

    await eventsApi.list({
      skip: 0,
      take: 20,
      startingOnOrAfter: new Date('2026-03-14T09:30:00Z'),
    })

    expect(lastUrl()).toBe(
      '/api/events?skip=0&take=20&startingOnOrAfter=2026-03-14T09%3A30%3A00.000Z',
    )
  })

  it('leaves the time bound out entirely when none is given', async () => {
    fetchMock.mockResolvedValue(pageResponse())

    await eventsApi.list({ skip: 40, take: 20 })

    expect(lastUrl()).toBe('/api/events?skip=40&take=20')
  })
})
