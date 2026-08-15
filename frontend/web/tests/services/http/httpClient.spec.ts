import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { httpClient, isAbortError } from '@/services/http/httpClient'

const originalFetch = globalThis.fetch

let fetchMock: ReturnType<typeof vi.fn>

function problemResponse(status: number, problem: unknown): Response {
  return new Response(JSON.stringify(problem), {
    status,
    headers: { 'content-type': 'application/problem+json' },
  })
}

function noContentResponse(): Response {
  return new Response(null, { status: 204 })
}

function abortError(): Error {
  return Object.assign(new Error('The operation was aborted.'), {
    name: 'AbortError',
  })
}

// A response whose headers arrived but whose body never finishes reading.
function bodyReadFailsResponse(status: number, cause: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers({ 'content-type': 'application/problem+json' }),
    text: () => Promise.reject(cause),
    json: () => Promise.reject(cause),
  } as unknown as Response
}

beforeEach(() => {
  fetchMock = vi.fn()
  globalThis.fetch = fetchMock as unknown as typeof fetch
})

afterEach(() => {
  globalThis.fetch = originalFetch
  vi.restoreAllMocks()
})

describe('isAbortError', () => {
  it('recognizes an abort by name, so one raised in another realm still counts', () => {
    expect(isAbortError(abortError())).toBe(true)
    expect(isAbortError({ name: 'AbortError' })).toBe(true)
  })

  it('rejects an ordinary failure, null, undefined and non-objects', () => {
    expect(isAbortError(new TypeError('Failed to fetch'))).toBe(false)
    expect(isAbortError(null)).toBe(false)
    expect(isAbortError(undefined)).toBe(false)
    expect(isAbortError('AbortError')).toBe(false)
  })
})

describe('httpClient', () => {
  it('forwards the caller signal so the request can be cancelled', async () => {
    const controller = new AbortController()

    fetchMock.mockResolvedValue(noContentResponse())

    await httpClient.get('/events', { signal: controller.signal })

    const init = fetchMock.mock.calls.at(-1)![1] as RequestInit

    expect(init.signal).toBe(controller.signal)
  })

  it('resolves to undefined when the response carries no body', async () => {
    fetchMock.mockResolvedValue(noContentResponse())

    await expect(httpClient.delete('/events/7')).resolves.toBeUndefined()
  })
})

describe('httpClient failure handling', () => {
  it('rejects with the problem detail and validation messages the API returned', async () => {
    fetchMock.mockResolvedValue(
      problemResponse(422, {
        title: 'Unprocessable entity',
        detail: 'The event has already started.',
        errors: { name: ['Name is required.'] },
      }),
    )

    await expect(
      httpClient.post('/events/7/register', {}),
    ).rejects.toMatchObject({
      status: 422,
      message: 'The event has already started.',
      errors: { name: ['Name is required.'] },
    })
  })

  it('rejects with a status message when the failure carried no JSON', async () => {
    fetchMock.mockResolvedValue(
      new Response('<html>oops</html>', { status: 500 }),
    )

    await expect(httpClient.get('/events')).rejects.toMatchObject({
      status: 500,
      message: 'Request failed with status 500.',
      errors: {},
    })
  })

  it('reports an unreachable API as an ApiError of status 0 and keeps the cause', async () => {
    const cause = new TypeError('Failed to fetch')

    fetchMock.mockRejectedValue(cause)

    await expect(httpClient.get('/events')).rejects.toMatchObject({
      name: 'ApiError',
      status: 0,
      message: 'The API could not be reached.',
      cause,
    })
  })

  it('rethrows a cancellation so callers can stay silent about it', async () => {
    const aborted = abortError()

    fetchMock.mockRejectedValue(aborted)

    await expect(httpClient.get('/events')).rejects.toBe(aborted)
  })

  it('reports a successful response whose body is not JSON as an ApiError, not a SyntaxError', async () => {
    fetchMock.mockResolvedValue(
      new Response('<html>a login page</html>', {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    )

    await expect(httpClient.get('/events')).rejects.toMatchObject({
      name: 'ApiError',
      status: 200,
      message: 'The API returned a body that could not be read as JSON.',
    })
  })

  it('keeps the failing status when the problem detail body is not the JSON it claims to be', async () => {
    fetchMock.mockResolvedValue(
      new Response('<html>gateway error</html>', {
        status: 500,
        headers: { 'content-type': 'application/problem+json' },
      }),
    )

    await expect(httpClient.get('/events')).rejects.toMatchObject({
      name: 'ApiError',
      status: 500,
      message: 'Request failed with status 500.',
      errors: {},
    })
  })

  it('still reports a cancellation as one when the request is aborted while the body is read', async () => {
    const aborted = abortError()

    fetchMock.mockResolvedValue(bodyReadFailsResponse(200, aborted))

    await expect(httpClient.get('/events')).rejects.toBe(aborted)

    fetchMock.mockResolvedValue(bodyReadFailsResponse(500, aborted))

    await expect(httpClient.get('/events')).rejects.toBe(aborted)
  })
})
