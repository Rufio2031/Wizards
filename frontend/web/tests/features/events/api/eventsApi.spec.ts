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

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

function lastCall(): [string, RequestInit] {
  const call = fetchMock.mock.calls.at(-1)

  expect(call).toBeDefined()

  return call as [string, RequestInit]
}

function lastUrl(): string {
  return lastCall()[0]
}

function lastBody(): unknown {
  return JSON.parse(lastCall()[1].body as string)
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

describe('eventsApi.register', () => {
  it('sends the player and the idempotency key that collapses retries', async () => {
    fetchMock.mockResolvedValue(jsonResponse({ name: 'Merlin' }, 201))

    const registration = await eventsApi.register('evt 1', {
      name: 'Merlin',
      idempotencyKey: '6f9619ff-8b86-d011-b42d-00cf4fc964ff',
    })

    expect(lastUrl()).toBe('/api/events/evt%201/registrations')
    expect(lastBody()).toEqual({
      name: 'Merlin',
      idempotencyKey: '6f9619ff-8b86-d011-b42d-00cf4fc964ff',
    })
    expect(registration).toEqual({ name: 'Merlin' })
  })

  it('refuses to report a seat the response never described', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(
      eventsApi.register('evt-1', {
        name: 'Merlin',
        idempotencyKey: '6f9619ff-8b86-d011-b42d-00cf4fc964ff',
      }),
    ).rejects.toThrow('Registering for the event returned no body.')
  })
})
