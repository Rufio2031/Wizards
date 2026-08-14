<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

import AppBadge from '@/components/AppBadge.vue'
import { formatSchedule } from '@/utils/dateTime'

import type { GameEvent } from '../types/event'

defineProps<{
  event: GameEvent
  to?: RouteLocationRaw
}>()
</script>

<template>
  <article class="event-card">
    <h2 class="event-card__name">
      <RouterLink v-if="to" class="event-card__link" :to="to">{{ event.name }}</RouterLink>

      <template v-else>{{ event.name }}</template>
    </h2>

    <p class="event-card__meta">
      {{ formatSchedule(event.startDateTime, event.endDateTime) }}
    </p>

    <p v-if="event.description" class="event-card__description">
      {{ event.description }}
    </p>

    <AppBadge class="event-card__game-type">
      {{ event.gameType.name }}
    </AppBadge>
  </article>
</template>

<style scoped>
.event-card {
  position: relative;
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-surface);
}

.event-card:has(.event-card__link):hover {
  box-shadow: var(--shadow-sm);
}

.event-card__name {
  margin: 0;
}

.event-card__link {
  color: inherit;
  text-decoration: none;
}

.event-card__link::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
}

.event-card__meta {
  margin-top: 8px;
  font-size: 0.875rem;
}

.event-card__description {
  margin-top: 16px;
  line-height: 1.6;
  color: var(--color-text-strong);
}

.event-card__game-type {
  margin-top: 16px;
}
</style>
