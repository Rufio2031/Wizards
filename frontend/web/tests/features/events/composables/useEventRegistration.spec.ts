import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, type EffectScope } from 'vue'

import { eventsApi } from '@/features/events/api/eventsApi'
import {
  useEventRegistration,
  type RegisterRequest,
} from '@/features/events/composables/useEventRegistration'
import type { Registration } from '@/features/events/types/event'
import { ApiError } from '@/services/http/ApiError'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, register: vi.fn() } }
})

const merlin: RegisterRequest = { name: 'Merlin' }

const seated: Registration = { name: 'Merlin' }

function deferred<T>() {
  let resolve!: (value: T) => void

  const promise = new Promise<T>((resolveIt) => {
    resolve = resolveIt
  })

  return { promise, resolve }
}

function sentKeys(): string[] {
  return vi
    .mocked(eventsApi.register)
    .mock.calls.map(([, request]) => request.idempotencyKey)
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

    const { registration, isRegistered, isSaving, failure, register } =
      runInScope(() => useEventRegistration('evt-1'))

    await expect(register(merlin)).resolves.toBeUndefined()

    expect(registration.value).toBeNull()
    expect(isRegistered.value).toBe(false)
    expect(isSaving.value).toBe(false)
    expect(failure.value).not.toBeNull()
  })

  it('joins the attempt already running instead of taking a second seat', async () => {
    const pending = deferred<Registration>()

    vi.mocked(eventsApi.register).mockReturnValue(pending.promise)

    const { isRegistered, register } = runInScope(() =>
      useEventRegistration('evt-1'),
    )

    const first = register(merlin)
    const second = register(merlin)

    pending.resolve(seated)

    await Promise.all([first, second])

    expect(eventsApi.register).toHaveBeenCalledTimes(1)
    expect(isRegistered.value).toBe(true)
  })

  it('reuses the same idempotency key when the player tries again after a failure', async () => {
    vi.mocked(eventsApi.register)
      .mockRejectedValueOnce(new ApiError(503, { detail: 'Registry offline.' }))
      .mockResolvedValueOnce(seated)

    const { registration, isRegistered, register } = runInScope(() =>
      useEventRegistration('evt-1'),
    )

    await register({ name: 'Merlin' })
    await register({ name: 'Merlyn' })

    const [firstKey, secondKey] = sentKeys()

    expect(firstKey).toBeTruthy()
    expect(secondKey).toBe(firstKey)

    expect(isRegistered.value).toBe(true)
    expect(registration.value).toEqual({ name: 'Merlin' })
  })

  it('gives each player their own idempotency key rather than one shared by the app', async () => {
    vi.mocked(eventsApi.register).mockResolvedValue(seated)

    const merlinSide = runInScope(() => useEventRegistration('evt-1'))
    const circeSide = runInScope(() => useEventRegistration('evt-1'))

    await merlinSide.register({ name: 'Merlin' })
    await circeSide.register({ name: 'Circe' })

    const [merlinKey, circeKey] = sentKeys()

    expect(merlinKey).toBeTruthy()
    expect(circeKey).not.toBe(merlinKey)
  })
})
