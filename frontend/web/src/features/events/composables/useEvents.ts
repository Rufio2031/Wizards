import { computed } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import { emptyPage, type Page } from '@/services/http/pagination'
import { toLocalDay } from '@/utils/dateTime'
import { groupBy } from '@/utils/grouping'

import { eventsApi } from '../api/eventsApi'
import type { GameEvent } from '../types/event'

const DEFAULT_PAGE_SIZE = 50

export interface EventDayGroup {
  key: string
  label: string
  events: GameEvent[]
}

function groupEventsByDay(events: readonly GameEvent[]): EventDayGroup[] {
  return groupBy(events, (event) => toLocalDay(event.startDateTime).key).map(
    ({ items }) => {
      const day = toLocalDay(items[0].startDateTime)

      return { key: day.key, label: day.label, events: items }
    },
  )
}

export function useEvents(pageSize: number = DEFAULT_PAGE_SIZE) {
  const {
    data,
    isLoading,
    error,
    refresh: load,
  } = useAsyncRequest<Page<GameEvent>>(
    (options) => eventsApi.list({ skip: 0, take: pageSize }, options),
    {
      initialValue: emptyPage<GameEvent>(pageSize),
      failureMessage: 'Loading events failed.',
    },
  )

  const events = computed(() => data.value.items)
  const eventGroups = computed(() => groupEventsByDay(events.value))
  const pagination = computed(() => data.value.pagination)

  return { events, eventGroups, pagination, isLoading, error, load }
}
