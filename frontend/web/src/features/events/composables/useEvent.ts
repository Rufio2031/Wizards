import { toValue, watch, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { eventsApi } from '../api/eventsApi'
import type { GameEvent } from '../types/event'

export function useEvent(eventId: MaybeRefOrGetter<string>) {
  const {
    data: event,
    isLoading,
    error,
    run: load,
  } = useAsyncRequest<GameEvent | null>(
    (options) => eventsApi.getById(toValue(eventId), options),
    {
      initialValue: null,
      failureMessage: 'Loading the event failed.',
    },
  )

  watch(
    () => toValue(eventId),
    () => {
      void load()
    },
    { immediate: true },
  )

  return { event, isLoading, error, load }
}
