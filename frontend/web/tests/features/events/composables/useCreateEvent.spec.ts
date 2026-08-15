import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import { useCreateEvent } from '@/features/events/composables/useCreateEvent'
import type {
  CreateEventRequest,
  GameEvent,
} from '@/features/events/types/event'
import { ApiError } from '@/services/http/ApiError'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, create: vi.fn() } }
})

const request: CreateEventRequest = {
  name: 'Summoning 101',
  location: 'The Tower',
  startDateTime: '2026-03-14T09:30:00',
  endDateTime: '2026-03-14T12:30:00',
  registrationLimit: 12,
  gameType: { gameTypeId: 'gt-1', selections: { difficulty: 'novice' } },
}

const created: GameEvent = {
  eventId: 'evt-1',
  name: 'Summoning 101',
  location: 'The Tower',
  startDateTime: '2026-03-14T09:30:00',
  endDateTime: '2026-03-14T12:30:00',
  registrationLimit: 12,
  gameType: { gameTypeId: 'gt-1', name: 'Ritual' },
  selections: { difficulty: 'novice' },
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

describe('useCreateEvent', () => {
  it('resolves to null and reports the blamed fields when the attempt fails', async () => {
    vi.mocked(eventsApi.create).mockRejectedValue(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: { name: ['Name is required.'] },
      }),
    )

    const { isSaving, failure, create } = runInScope(() => useCreateEvent())

    await expect(create({ ...request, name: '' })).resolves.toBeNull()

    expect(isSaving.value).toBe(false)
    expect(failure.value).toEqual({
      fieldErrors: { name: ['Name is required.'] },
      formMessages: [],
    })
  })

  it('names a value the API could not read, without its serializer text', async () => {
    vi.mocked(eventsApi.create).mockRejectedValue(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          '$.startDateTime': [
            'The JSON value could not be converted to System.DateTimeOffset. Path: $.startDateTime | LineNumber: 0 | BytePositionInLine: 52.',
          ],
        },
      }),
    )

    const { failure, create } = runInScope(() => useCreateEvent())

    await expect(create({ ...request, startDateTime: 'the ides of March' })).resolves.toBeNull()

    expect(failure.value).toEqual({
      fieldErrors: {
        startDateTime: ['We could not read this value. Please check it and try again.'],
      },
      formMessages: [],
    })
  })

  it('joins the attempt already running instead of creating a second event', async () => {
    const pending = deferred<GameEvent>()

    vi.mocked(eventsApi.create).mockReturnValue(pending.promise)

    const { create } = runInScope(() => useCreateEvent())

    const first = create(request)
    const second = create(request)

    pending.resolve(created)

    await expect(first).resolves.toEqual(created)
    await expect(second).resolves.toEqual(created)
    expect(eventsApi.create).toHaveBeenCalledTimes(1)
  })

  it('lets the organizer try again after a failed attempt', async () => {
    vi.mocked(eventsApi.create)
      .mockRejectedValueOnce(new ApiError(503, { detail: 'Registry offline.' }))
      .mockResolvedValueOnce(created)

    const { create } = runInScope(() => useCreateEvent())

    await expect(create(request)).resolves.toBeNull()
    await expect(create(request)).resolves.toEqual(created)
  })
})
