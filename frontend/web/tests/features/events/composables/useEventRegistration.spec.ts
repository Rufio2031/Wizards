import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import { useEventRegistration } from '@/features/events/composables/useEventRegistration'
import type { CreateRegistrationRequest } from '@/features/events/types/event'
import { ApiError } from '@/services/http/ApiError'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, register: vi.fn() } }
})

const merlin: CreateRegistrationRequest = { name: 'Merlin' }

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

describe('useEventRegistration', () => {
  it('leaves the player unregistered without rejecting when the attempt fails', async () => {
    vi.mocked(eventsApi.register).mockRejectedValue(
      new ApiError(409, { detail: 'The event is full.' }),
    )

    const { isRegistered, isSaving, failure, register } = runInScope(() =>
      useEventRegistration('evt-1'),
    )

    await expect(register(merlin)).resolves.toBeUndefined()

    expect(isRegistered.value).toBe(false)
    expect(isSaving.value).toBe(false)
    expect(failure.value).not.toBeNull()
  })

  it('joins the attempt already running instead of taking a second seat', async () => {
    const pending = deferred<void>()

    vi.mocked(eventsApi.register).mockReturnValue(pending.promise)

    const { isRegistered, register } = runInScope(() =>
      useEventRegistration('evt-1'),
    )

    const first = register(merlin)
    const second = register(merlin)

    pending.resolve()

    await Promise.all([first, second])

    expect(eventsApi.register).toHaveBeenCalledTimes(1)
    expect(isRegistered.value).toBe(true)
  })

  it('lets the player try again after a failed attempt', async () => {
    vi.mocked(eventsApi.register)
      .mockRejectedValueOnce(new ApiError(503, { detail: 'Registry offline.' }))
      .mockResolvedValueOnce(undefined)

    const { isRegistered, register } = runInScope(() =>
      useEventRegistration('evt-1'),
    )

    await register(merlin)
    await register(merlin)

    expect(isRegistered.value).toBe(true)
  })
})
