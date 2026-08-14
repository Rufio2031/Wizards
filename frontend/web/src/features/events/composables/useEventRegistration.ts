import { toValue, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { eventsApi } from '../api/eventsApi'
import type { CreateRegistrationRequest } from '../types/event'

/**
 * Registers a player for an event and reports why an attempt failed.
 *
 * @param eventId The event being registered for.
 * @returns `isRegistered`, true once an attempt has succeeded, `isSaving`, the
 * `failure` from the last attempt with its messages split by the field the API
 * blamed, `register` to make an attempt, and `clearFailure` to drop a reported
 * failure once the player starts correcting it.
 */
export function useEventRegistration(eventId: MaybeRefOrGetter<string>) {
  const {
    data: isRegistered,
    isLoading: isSaving,
    failure,
    clearError: clearFailure,
    run,
  } = useAsyncRequest<boolean, CreateRegistrationRequest>(
    async (options, request) => {
      await eventsApi.register(toValue(eventId), request, options)

      return true
    },
    { initialValue: false, failureMessage: 'Registering for the event failed.' },
  )

  let inFlight: Promise<boolean | null> | null = null

  /**
   * Registers the player, or joins the attempt already running.
   *
   * Registering is not idempotent, so a double-tapped button must not start a
   * second request. Aborting the first would only stop this client from hearing
   * the answer: the server may already have taken the seat, leaving the player
   * registered twice and a page that thinks they failed.
   *
   * @param request The player to register.
   */
  async function register(request: CreateRegistrationRequest): Promise<void> {
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

  return { isRegistered, isSaving, failure, register, clearFailure }
}
