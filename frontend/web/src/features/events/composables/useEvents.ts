import { computed } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import { emptyPage, type Page } from '@/services/http/pagination'

import { eventsApi } from '../api/eventsApi'
import type { GameEvent } from '../types/event'

const PAGE_SIZE = 10

export function useEvents() {
  const {
    data,
    isLoading,
    error,
    refresh: load,
  } = useAsyncRequest<Page<GameEvent>>(
    (options) => eventsApi.list({ skip: 0, take: PAGE_SIZE }, options),
    {
      initialValue: emptyPage<GameEvent>(PAGE_SIZE),
      failureMessage: 'Loading events failed.',
    },
  )

  const events = computed(() => data.value.items)

  return { events, isLoading, error, load }
}
