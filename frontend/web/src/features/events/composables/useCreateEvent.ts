import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { eventsApi } from '../api/eventsApi'
import type { CreateEventRequest, GameEvent } from '../types/event'

/**
 * Creates an event and reports why an attempt failed.
 *
 * @returns `isSaving`, the `failure` from the last attempt with its messages
 * split by the field the API blamed, and `create`, which resolves to the created
 * event or to `null` when the attempt failed.
 */
export function useCreateEvent() {
  const {
    isLoading: isSaving,
    failure,
    run,
  } = useAsyncRequest<GameEvent | null, CreateEventRequest>(
    (options, request) => eventsApi.create(request, options),
    { initialValue: null, failureMessage: 'Creating an event failed.' },
  )

  let inFlight: Promise<GameEvent | null> | null = null

  /**
   * Creates an event, or joins the attempt already running.
   *
   * Creating is not idempotent, so a second call while one is in flight must not
   * start a second request. Aborting the first would only stop this client from
   * hearing the answer: the server may already have committed it, leaving two
   * events and a page that knows about one.
   *
   * @param request The event to create.
   * @returns The created event, or `null` when the attempt failed.
   */
  async function create(request: CreateEventRequest): Promise<GameEvent | null> {
    if (inFlight) {
      return inFlight
    }

    inFlight = run(request)

    try {
      return await inFlight
    } finally {
      inFlight = null
    }
  }

  return { isSaving, failure, create }
}
