<script setup lang="ts">
// Renders one event's details and how many seats are left on it.
import { computed } from 'vue'

import type { GameEvent } from '../data/events.types'

const props = defineProps<{
  /** The event to display. */
  event: GameEvent
}>()

const formattedDate = computed(() =>
  new Date(`${props.event.date}T00:00:00`).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }),
)

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
