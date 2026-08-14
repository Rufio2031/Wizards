import { computed, onScopeDispose, ref, shallowRef } from 'vue'

import { isApiError } from '@/services/http/ApiError'
import { isAbortError, type RequestOptions } from '@/services/http/httpClient'
import { toApiFailure } from '@/services/http/validation'

export interface UseAsyncRequestOptions<TData> {
  /** Value `data` holds until the first response arrives. */
  initialValue: TData

  /** Logged with the raw failure, which never reaches a template. */
  failureMessage: string
}

/**
 * Runs a cancellable API call and owns its loading, error, and data state.
 *
 * A new run aborts the previous one and only the newest run writes state.
 * Aborting does not undo a write the server already received, so a
 * non-idempotent `send` needs its own guard against a second run. The in-flight
 * call is aborted when the owning scope is disposed.
 *
 * @param send Performs the request with an abort signal and the run's arguments.
 * @param options Initial `data` value and the message logged on failure.
 * @returns `data`, `isLoading`, `error`, `failure`, `dataNotFound`, `run`, and `clearError`.
 */
export function useAsyncRequest<TData, TArgs = void>(
  send: (options: RequestOptions, args: TArgs) => Promise<TData>,
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

  async function run(args: TArgs): Promise<TData | null> {
    controller?.abort()
    controller = new AbortController()

    const runId = ++latestRunId

    isLoading.value = true
    error.value = null

    try {
      const result = await send({ signal: controller.signal }, args)

      if (!canWrite(runId)) {
        return null
      }

      data.value = result

      return result
    } catch (caught) {
      if (isAbortError(caught) || !canWrite(runId)) {
        return null
      }

      console.error(failureMessage, caught)

      error.value = caught instanceof Error ? caught : new Error(failureMessage)

      return null
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

  const failure = computed(() => toApiFailure(error.value))

  const dataNotFound = computed(
    () => isApiError(error.value) && error.value.status === 404,
  )

  /** Drops the last failure, leaving `data` as it was. */
  function clearError() {
    error.value = null
  }

  return { data, isLoading, error, dataNotFound, failure, clearError, run }
}
