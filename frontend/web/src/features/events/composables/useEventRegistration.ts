import { computed, toValue, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import { createUuid } from '@/utils/uuid'

import { eventsApi } from '../api/eventsApi'
import type { CreateRegistrationRequest, Registration } from '../types/event'

/** The composable owns the idempotency key, so a caller never supplies one. */
export type RegisterRequest = Omit<CreateRegistrationRequest, 'idempotencyKey'>

/**
 * Registers a player for an event and reports why an attempt failed.
 *
 * @param eventId The event being registered for.
 * @returns `registration`, the stored registration once an attempt has
 * succeeded, `isRegistered`, `isSaving`, the `failure` from the last attempt
 * with its messages split by the field the API blamed, `register` to make an
 * attempt, and `clearFailure` to drop a reported failure once the player starts
 * correcting it.
 */
export function useEventRegistration(eventId: MaybeRefOrGetter<string>) {
  const idempotencyKey = createUuid()

  const {
    data: registration,
    isLoading: isSaving,
    failure,
    clearError: clearFailure,
    run,
  } = useAsyncRequest<Registration | null, RegisterRequest>(
    (options, request) =>
      eventsApi.register(
        toValue(eventId),
        { ...request, idempotencyKey },
        options,
      ),
    {
      initialValue: null,
      failureMessage: 'Registering for the event failed.',
    },
  )

  const isRegistered = computed(() => registration.value !== null)

  let inFlight: Promise<Registration | null> | null = null

  /**
   * Registers the player, or joins the attempt already running.
   *
   * The idempotency key makes a second request harmless, so the guard is only
   * there to avoid a redundant call from a double-tapped button. Aborting the
   * first would only stop this client from hearing the answer, leaving a player
   * the server has already seated looking at a page that thinks they failed.
   *
   * @param request The player to register.
   */
  async function register(request: RegisterRequest): Promise<void> {
    if (inFlight) {
      await inFlight

      return
    }

    inFlight = run(request)

    try {
      await inFlight
    } finally {
      inFlight = null
    }
  }

  return {
    registration,
    isRegistered,
    isSaving,
    failure,
    register,
    clearFailure,
  }
}
