import { onScopeDispose, ref, shallowRef } from 'vue'

import { isAbortError, type RequestOptions } from '@/services/http/httpClient'

export interface UseAsyncRequestOptions<TData> {
  /** Value `data` holds until the first response arrives. */
  initialValue: TData

  /** Logged with the raw failure, which never reaches a template. */
  failureMessage: string
}

/**
 * Runs a cancellable API call and owns its loading, error, and data state.
 *
 * @param send Performs the call, forwarding the abort signal it is handed.
 * @param options `initialValue` for `data` and the `failureMessage` logged on
 * failure.
 * @returns `data`, `isLoading`, `error` from the last failure (a log detail,
 * not display copy), and `refresh` to run the call again. A run aborts the
 * previous one, only the newest undisposed run may write state, and the
 * in-flight call is aborted when the owning scope is disposed.
 */
export function useAsyncRequest<TData>(
  send: (options: RequestOptions) => Promise<TData>,
  { initialValue, failureMessage }: UseAsyncRequestOptions<TData>,
) {
  const data = shallowRef<TData>(initialValue)
  const isLoading = ref(false)
  const error = shallowRef<Error | null>(null)

  let controller: AbortController | null = null
  let latestRunId = 0
  let isDisposed = false

  // Aborting is best effort: a call already in flight can still settle, so
  // these guards are what actually keep stale or discarded runs from writing.
  const canWrite = (runId: number) => !isDisposed && runId === latestRunId

  async function refresh(): Promise<void> {
    controller?.abort()
    controller = new AbortController()

    const runId = ++latestRunId

    isLoading.value = true
    error.value = null

    try {
      const result = await send({ signal: controller.signal })

      if (canWrite(runId)) {
        data.value = result
      }
    } catch (caught) {
      if (isAbortError(caught) || !canWrite(runId)) {
        return
      }

      console.error(failureMessage, caught)

      error.value = caught instanceof Error ? caught : new Error(failureMessage)
    } finally {
      if (canWrite(runId)) {
        isLoading.value = false
      }
    }
  }

  onScopeDispose(() => {
    isDisposed = true
    controller?.abort()
  })

  return { data, isLoading, error, refresh }
}
