import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, type EffectScope } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import { ApiError } from '@/services/http/ApiError'
import type { RequestOptions } from '@/services/http/httpClient'

const FAILURE_MESSAGE = 'Loading the game type failed.'

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

function abortError(): Error {
  return Object.assign(new Error('The operation was aborted.'), {
    name: 'AbortError',
  })
}

const scopes: EffectScope[] = []

function inScope<T>(factory: () => T): T {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(factory) as T
}

function create<TData>(
  send: (options: RequestOptions) => Promise<TData>,
  initialValue: TData,
) {
  return inScope(() =>
    useAsyncRequest<TData>(send, {
      initialValue,
      failureMessage: FAILURE_MESSAGE,
    }),
  )
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

describe('useAsyncRequest failure', () => {
  it('exposes the failure, returns null and keeps the previous data', async () => {
    const failure = new ApiError(500, { title: 'Server error' })
    let attempt = 0

    const request = create(async () => {
      attempt += 1

      if (attempt === 1) {
        return 'Chess'
      }

      throw failure
    }, '')

    await request.run()

    await expect(request.run()).resolves.toBeNull()

    expect(request.error.value).toBe(failure)
    expect(request.data.value).toBe('Chess')
    expect(request.isLoading.value).toBe(false)
  })

  it('reports the failure message when something other than an error was thrown', async () => {
    const request = create(async () => {
      throw 'kaboom'
    }, '')

    await request.run()

    expect(request.error.value).toBeInstanceOf(Error)
    expect(request.error.value?.message).toBe('Loading the game type failed.')
  })

  it('clears the previous failure when a new run starts', async () => {
    const pending = deferred<string>()
    let attempt = 0

    const request = create(() => {
      attempt += 1

      return attempt === 1 ? Promise.reject(new ApiError(500)) : pending.promise
    }, '')

    await request.run()

    expect(request.error.value).not.toBeNull()

    const running = request.run()

    await nextTick()

    expect(request.error.value).toBeNull()

    pending.resolve('Chess')

    await running
  })

  it('stays silent when the request was cancelled', async () => {
    const request = create(async () => {
      throw abortError()
    }, '')

    await expect(request.run()).resolves.toBeNull()

    expect(request.error.value).toBeNull()
    expect(request.isLoading.value).toBe(false)
    expect(console.error).not.toHaveBeenCalled()
  })

  it('reports a missing resource for a 404 and for no other failure', async () => {
    const missing = create(async () => {
      throw new ApiError(404, { title: 'Not Found' })
    }, '')

    const broken = create(async () => {
      throw new ApiError(500, { title: 'Server error' })
    }, '')

    await missing.run()
    await broken.run()

    expect(missing.dataNotFound.value).toBe(true)
    expect(broken.dataNotFound.value).toBe(false)
  })
})

describe('useAsyncRequest overlapping runs', () => {
  it('cancels the run already in flight', async () => {
    const signals: AbortSignal[] = []
    const pending = deferred<string>()

    const request = create((options: RequestOptions) => {
      signals.push(options.signal as AbortSignal)

      return pending.promise
    }, '')

    void request.run()
    void request.run()

    expect(signals[0].aborted).toBe(true)
    expect(signals[1].aborted).toBe(false)

    pending.resolve('Chess')
    await flushPromises()
  })

  it('keeps the newest result when an older run settles last', async () => {
    const first = deferred<string>()
    const second = deferred<string>()
    const pendings = [first, second]
    let attempt = 0

    const request = create(() => pendings[attempt++].promise, '')

    const firstRun = request.run()
    const secondRun = request.run()

    second.resolve('Poker')
    await expect(secondRun).resolves.toBe('Poker')

    first.resolve('Chess')

    await expect(firstRun).resolves.toBeNull()
    expect(request.data.value).toBe('Poker')
  })

  it('ignores a failure from a run that was superseded', async () => {
    const first = deferred<string>()
    const second = deferred<string>()
    const pendings = [first, second]
    let attempt = 0

    const request = create(() => pendings[attempt++].promise, '')

    const firstRun = request.run()
    const secondRun = request.run()

    second.resolve('Poker')
    await secondRun

    first.reject(new ApiError(500))

    await expect(firstRun).resolves.toBeNull()
    expect(request.error.value).toBeNull()
    expect(request.data.value).toBe('Poker')
    expect(request.isLoading.value).toBe(false)
  })
})

describe('useAsyncRequest scope disposal', () => {
  it('cancels the request in flight when the owning scope is stopped', async () => {
    const pending = deferred<string>()
    let signal: AbortSignal | undefined

    const scope = effectScope()

    const request = scope.run(() =>
      useAsyncRequest(
        (options: RequestOptions) => {
          signal = options.signal

          return pending.promise
        },
        { initialValue: '', failureMessage: FAILURE_MESSAGE },
      ),
    )!

    void request.run()

    scope.stop()

    expect(signal?.aborted).toBe(true)

    pending.resolve('Chess')
    await flushPromises()
  })

  it('writes no data once the owning scope is stopped', async () => {
    const pending = deferred<string>()

    const scope = effectScope()

    const request = scope.run(() =>
      useAsyncRequest(() => pending.promise, {
        initialValue: '',
        failureMessage: FAILURE_MESSAGE,
      }),
    )!

    const running = request.run()

    scope.stop()

    pending.resolve('Chess')

    await expect(running).resolves.toBeNull()
    expect(request.data.value).toBe('')
  })
})
