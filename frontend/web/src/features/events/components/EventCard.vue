<script setup lang="ts">
// Summary card for one event: name, date, venue, and remaining seats.
import { computed } from 'vue'

import type { GameEvent } from '../types/event'

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/

const UNKNOWN_DATE_LABEL = 'Date to be announced'

// Module level so a formatter is built once, not per card per render.
const DATE_FORMAT: Intl.DateTimeFormatOptions = {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
}

const dateFormatter = new Intl.DateTimeFormat(undefined, DATE_FORMAT)

const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  ...DATE_FORMAT,
  hour: 'numeric',
  minute: '2-digit',
})

const props = defineProps<{
  /** The event to display. */
  event: GameEvent
}>()

const formattedDate = computed(() => {
  const raw = (props.event.date ?? '').trim()
  const isDateOnly = DATE_ONLY.test(raw)

  // A bare `YYYY-MM-DD` parses as UTC midnight, which renders as the previous
  // day west of Greenwich, so pin it to local midnight instead.
  const parsed = new Date(isDateOnly ? `${raw}T00:00:00` : raw)

  if (Number.isNaN(parsed.getTime())) {
    return UNKNOWN_DATE_LABEL
  }

  return isDateOnly
    ? dateFormatter.format(parsed)
    : dateTimeFormatter.format(parsed)
})

// Clamped because registration counts will come from an API that may report a
// closed event as over capacity.
const seatsLeft = computed(() =>
  Math.max(0, props.event.capacity - props.event.registered),
)

const isFull = computed(() => seatsLeft.value === 0)

const seatsLabel = computed(() =>
  isFull.value ? 'Full' : `${seatsLeft.value} seats left`,
)
</script>

<template>
  <article class="event-card">
    <h2 class="event-card__name">{{ event.name }}</h2>

    <p class="event-card__meta">{{ formattedDate }} &middot; {{ event.location }}</p>
    <p class="event-card__meta">
      {{ event.registered }} of {{ event.capacity }} registered
    </p>

    <p class="event-card__seats" :class="{ 'event-card__seats--full': isFull }">
      {{ seatsLabel }}
    </p>
  </article>
</template>

<style scoped>
.event-card {
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-surface);
}

.event-card__name {
  margin-bottom: 4px;
}

.event-card__meta {
  font-size: 0.875rem;
}

.event-card__seats {
  display: inline-block;
  margin-top: 8px;
  padding: 4px 12px;
  /* Transparent rather than absent so every variant is the same size. */
  border: 1px solid transparent;
  border-radius: 999px;
  font-size: 0.875rem;
  color: var(--color-accent);
  background: var(--color-accent-soft);
}

/* Achromatic: a full event is unavailable, not an error. */
.event-card__seats--full {
  color: var(--color-status-muted);
  background: var(--color-status-muted-soft);
}
</style>
