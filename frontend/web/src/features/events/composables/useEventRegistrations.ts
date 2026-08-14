import { toValue, watch, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { eventsApi } from '../api/eventsApi'
import type { Registration } from '../types/event'

/**
 * Reads the players registered for an event.
 *
 * @param eventId The event to read registrations for.
 * @returns `registrations` in the order they were taken, `isLoading`, `error`
 * from the last attempt, and `load` to read them again.
 */
export function useEventRegistrations(eventId: MaybeRefOrGetter<string>) {
  const {
    data: registrations,
    isLoading,
    error,
    run: load,
  } = useAsyncRequest<Registration[]>(
    (options) => eventsApi.listRegistrations(toValue(eventId), options),
    {
      initialValue: [],
      failureMessage: 'Loading the registrations failed.',
    },
  )

  watch(
    () => toValue(eventId),
    () => {
      void load()
    },
    { immediate: true },
  )

  return { registrations, isLoading, error, load }
}
