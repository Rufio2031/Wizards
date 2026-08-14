import { computed, onScopeDispose, ref, shallowRef } from 'vue'

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
 * A new run aborts the previous one and only the newest run writes state, which
 * suits reads where only the latest answer matters. Aborting does not undo a
 * write the server already received, so a caller with a non-idempotent `send`
 * must stop a second run from starting rather than rely on the abort.
 *
 * @param send Performs the call, forwarding the abort signal it is handed and
 * whatever `run` was called with.
 * @param options `initialValue` for `data` and the `failureMessage` logged on
 * failure.
 * @returns `data`, `isLoading`, `error` from the last failure (a detail to map
 * or log, never display copy), `failure` carrying that error's validation
 * messages split by field, `run` to perform the call, and `clearError` to drop
 * a reported failure without running again. The in-flight call is aborted when
 * the owning scope is disposed.
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

  /** Drops the last failure, leaving `data` as it was. */
  function clearError() {
    error.value = null
  }

  return { data, isLoading, error, failure, clearError, run }
}
