<script setup lang="ts">
import { computed } from 'vue'

import { formatDateTimeRange } from '@/utils/dateTime'

import type { GameEvent } from '../types/event'

const UNKNOWN_SCHEDULE_LABEL = 'Date to be announced'

const props = defineProps<{
  event: GameEvent
}>()

const schedule = computed(
  () =>
    formatDateTimeRange(props.event.startDateTime, props.event.endDateTime) ??
    UNKNOWN_SCHEDULE_LABEL,
)
</script>

<template>
  <article class="event-card">
    <h2 class="event-card__name">{{ event.name }}</h2>

    <p class="event-card__meta">{{ schedule }}</p>

    <p v-if="event.description" class="event-card__description">
      {{ event.description }}
    </p>

    <p class="event-card__game-type">{{ event.gameType.name }}</p>
  </article>
</template>

<style scoped>
.event-card {
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-surface);
}

.event-card__name {
  margin-bottom: 4px;
}

.event-card__meta {
  font-size: 0.875rem;
}

.event-card__description {
  margin-top: 8px;
}

.event-card__game-type {
  display: inline-block;
  margin-top: 8px;
  padding: 4px 12px;
  border-radius: 999px;
  font-size: 0.875rem;
  color: var(--color-accent);
  background: var(--color-accent-soft);
}
</style>
