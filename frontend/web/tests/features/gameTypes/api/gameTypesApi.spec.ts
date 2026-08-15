import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { gameTypesApi } from '@/features/gameTypes/api/gameTypesApi'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'

const originalFetch = globalThis.fetch

let fetchMock: ReturnType<typeof vi.fn>

const ritual: GameTypeTemplate = {
  gameTypeId: 'gt 1',
  name: 'Ritual',
  settings: [
    {
      key: 'candles',
      label: 'Candles',
      type: 'int',
      defaultValue: '3',
      options: [],
    },
  ],
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

function lastUrl(): string {
  const call = fetchMock.mock.calls.at(-1)

  expect(call).toBeDefined()

  return (call as [string, RequestInit])[0]
}

beforeEach(() => {
  fetchMock = vi.fn()
  globalThis.fetch = fetchMock as unknown as typeof fetch
})

afterEach(() => {
  globalThis.fetch = originalFetch
  vi.restoreAllMocks()
})

describe('gameTypesApi.list', () => {
  it('asks the game types collection for every template', async () => {
    fetchMock.mockResolvedValue(jsonResponse([ritual]))

    const gameTypes = await gameTypesApi.list()

    expect(lastUrl()).toBe('/api/gametypes')
    expect(gameTypes).toEqual([ritual])
  })
})

describe('gameTypesApi.getById', () => {
  it('asks for the one game type, with its id escaped into the path', async () => {
    fetchMock.mockResolvedValue(jsonResponse(ritual))

    const gameType = await gameTypesApi.getById('gt 1')

    expect(lastUrl()).toBe('/api/gametypes/gt%201')
    expect(gameType).toEqual(ritual)
  })

  it('refuses to report a game type the response never described', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(gameTypesApi.getById('gt-1')).rejects.toThrow(
      'The game type endpoint returned no body.',
    )
  })
})
