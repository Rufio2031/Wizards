<script setup lang="ts">
import AppAction from '@/components/AppAction.vue'
import { RouteNames } from '@/router/routeNames'

import EventCard from '../components/EventCard.vue'
import { useEvents } from '../composables/useEvents'

const { events, isLoading, error, load } = useEvents()

load()
</script>

<template>
  <section class="events">
    <h1 class="events__title">Events</h1>

    <p v-if="isLoading">Loading events…</p>

    <template v-else-if="error">
      <p>We could not load events just now. Please try again.</p>

      <AppAction class="events__retry" @click="load">Try again</AppAction>
    </template>

    <p v-else-if="events.length === 0">No events are scheduled yet.</p>

    <ul v-else class="events__list">
      <li v-for="event in events" :key="event.eventId">
        <EventCard
          :event="event"
          :to="{ name: RouteNames.eventDetail, params: { eventId: event.eventId } }"
        />
      </li>
    </ul>
  </section>
</template>

<style scoped>
.events__title {
  margin-top: 0;
}

.events__retry {
  margin-top: 16px;
}

.events__list {
  display: flex;
  flex-direction: column;
  gap: 24px;
  margin: 0;
  padding: 0;
  list-style: none;
}
</style>
