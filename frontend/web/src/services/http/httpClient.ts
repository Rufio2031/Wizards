import { env } from '@/config/env'

import { ApiError, type ProblemDetails } from './ApiError'

export interface RequestOptions {
  signal?: AbortSignal
}

const NETWORK_PROBLEM: ProblemDetails = {
  title: 'Network error',
  detail: 'The API could not be reached.',
}

/**
 * True when a rejection came from a caller-initiated abort rather than a
 * failure. Callers use it to stay silent instead of showing an error.
 *
 * @param error The caught rejection value.
 * @returns `true` for an abort, `false` for anything else.
 */
export function isAbortError(error: unknown): boolean {
  // Errors from other realms fail `instanceof DOMException`, so match the name
  // every implementation sets.
  return (
    typeof error === 'object' &&
    error !== null &&
    (error as { name?: unknown }).name === 'AbortError'
  )
}

async function readProblemDetails(response: Response): Promise<ProblemDetails> {
  // Covers application/problem+json too.
  if (!(response.headers.get('content-type') ?? '').includes('json')) {
    return {}
  }

  return (await response.json()) as ProblemDetails
}

async function request<TResponse>(
  method: string,
  path: string,
  body?: unknown,
  options: RequestOptions = {},
): Promise<TResponse | undefined> {
  const headers: Record<string, string> = { Accept: 'application/json' }

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  let response: Response

  try {
    response = await fetch(`${env.apiBasePath}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: options.signal,
    })
  } catch (caught) {
    // An abort stays a distinct signal so callers can tell cancelled from
    // failed; anything else becomes an ApiError so no raw TypeError from a
    // dropped connection reaches a caller. Status 0: no response ever arrived.
    if (isAbortError(caught)) {
      throw caught
    }

    throw new ApiError(0, NETWORK_PROBLEM, { cause: caught })
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblemDetails(response))
  }

  const text = await response.text()

  return text.length === 0 ? undefined : (JSON.parse(text) as TResponse)
}

/**
 * The single place `fetch` is called. Feature API modules own the paths.
 *
 * Every method resolves to `undefined` when the response carried no body, so
 * call sites decide what an empty response means for their own return type.
 */
export const httpClient = {
  get<TResponse>(
    path: string,
    options?: RequestOptions,
  ): Promise<TResponse | undefined> {
    return request<TResponse>('GET', path, undefined, options)
  },

  post<TResponse>(
    path: string,
    body?: unknown,
    options?: RequestOptions,
  ): Promise<TResponse | undefined> {
    return request<TResponse>('POST', path, body, options)
  },

  put<TResponse>(
    path: string,
    body: unknown,
    options?: RequestOptions,
  ): Promise<TResponse | undefined> {
    return request<TResponse>('PUT', path, body, options)
  },

  patch<TResponse>(
    path: string,
    body: unknown,
    options?: RequestOptions,
  ): Promise<TResponse | undefined> {
    return request<TResponse>('PATCH', path, body, options)
  },

  delete<TResponse = void>(
    path: string,
    options?: RequestOptions,
  ): Promise<TResponse | undefined> {
    return request<TResponse>('DELETE', path, undefined, options)
  },
}
