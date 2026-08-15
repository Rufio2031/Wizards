import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import { useEvent } from '@/features/events/composables/useEvent'
import type { GameEvent } from '@/features/events/types/event'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, getById: vi.fn() } }
})

const summoning: GameEvent = {
  eventId: 'evt-1',
  name: 'Summoning 101',
  location: 'The Tower',
  startDateTime: '2026-03-14T09:30:00',
  endDateTime: '2026-03-14T12:30:00',
  registrationLimit: 12,
  gameType: { gameTypeId: 'gt-1', name: 'Ritual' },
  selections: { difficulty: 'novice' },
}

const alchemy: GameEvent = {
  eventId: 'evt-2',
  name: 'Alchemy Night',
  location: 'The Cellar',
  startDateTime: '2026-03-15T18:00:00',
  endDateTime: '2026-03-15T21:00:00',
  registrationLimit: 8,
  gameType: { gameTypeId: 'gt-2', name: 'Crafting' },
  selections: {},
}

function deferred<T>() {
  let resolve!: (value: T) => void

  const promise = new Promise<T>((resolveIt) => {
    resolve = resolveIt
  })

  return { promise, resolve }
}

const scopes: EffectScope[] = []

function runInScope<T>(factory: () => T): T {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(factory)!
}

beforeEach(() => {
  vi.resetAllMocks()
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  while (scopes.length) {
    scopes.pop()!.stop()
  }

  vi.restoreAllMocks()
})

describe('useEvent', () => {
  it('loads the event the id names, keeping the newest when an earlier request settles last', async () => {
    const slow = deferred<GameEvent>()

    vi.mocked(eventsApi.getById).mockImplementation(async (id: string) =>
      id === 'evt-1' ? slow.promise : alchemy,
    )

    const eventId = ref('evt-1')
    const { event } = runInScope(() => useEvent(eventId))

    eventId.value = 'evt-2'
    await flushPromises()

    slow.resolve(summoning)
    await flushPromises()

    expect(event.value).toEqual(alchemy)
  })

  it('offers a calendar invite only once an event has loaded', async () => {
    const pending = deferred<GameEvent>()

    vi.mocked(eventsApi.getById).mockReturnValue(pending.promise)

    const { calendarInviteUrl } = runInScope(() => useEvent('evt-1'))

    expect(calendarInviteUrl.value).toBeUndefined()

    pending.resolve(summoning)
    await flushPromises()

    expect(calendarInviteUrl.value).toBe('/api/events/evt-1/calendar.ics')
  })
})
