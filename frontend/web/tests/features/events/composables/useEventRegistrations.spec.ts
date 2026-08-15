import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import { useEventRegistrations } from '@/features/events/composables/useEventRegistrations'
import type { Registration } from '@/features/events/types/event'

vi.mock('@/features/events/api/eventsApi', () => ({
  eventsApi: { listRegistrations: vi.fn() },
}))

const listRegistrations = vi.mocked(eventsApi.listRegistrations)

const summoningRoster: Registration[] = [
  { name: 'Merlin' },
  { name: 'Morgana' },
]
const alchemyRoster: Registration[] = [{ name: 'Nicolas' }]

const scopes: EffectScope[] = []

function create(...args: Parameters<typeof useEventRegistrations>) {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(() => useEventRegistrations(...args))!
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  for (const scope of scopes.splice(0)) {
    scope.stop()
  }

  vi.restoreAllMocks()
  listRegistrations.mockReset()
})

describe('useEventRegistrations', () => {
  it('reads the roster for the event it is given without being asked, and again when the event changes', async () => {
    listRegistrations.mockImplementation(async (eventId: string) =>
      eventId === 'evt-1' ? summoningRoster : alchemyRoster,
    )

    const eventId = ref('evt-1')
    const { registrations } = create(eventId)

    expect(registrations.value).toEqual([])

    await flushPromises()

    expect(registrations.value).toEqual([
      { name: 'Merlin' },
      { name: 'Morgana' },
    ])

    eventId.value = 'evt-2'
    await flushPromises()

    expect(registrations.value).toEqual([{ name: 'Nicolas' }])
  })
})
