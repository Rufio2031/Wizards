<script setup lang="ts">
import AppAction from '@/components/AppAction.vue'
import AppAsyncState from '@/components/AppAsyncState.vue'
import { RouteNames } from '@/router/routeNames'

import EventCard from '../components/EventCard.vue'
import { useEvents } from '../composables/useEvents'

const { eventGroups, isLoading, error, load } = useEvents()

load()
</script>

<template>
  <section class="events">
    <div class="events__header">
      <h1 class="events__title">Events</h1>

      <AppAction :to="{ name: RouteNames.eventCreate }">Schedule an event</AppAction>
    </div>

    <AppAsyncState
      v-slot="{ data: groups }"
      :data="eventGroups"
      :loading="isLoading"
      :failed="!!error"
      loading-text="Loading events…"
      error-text="We could not load events just now. Please try again."
      @retry="load"
    >
      <p v-if="groups.length === 0">No events are scheduled yet.</p>

      <ul v-else class="events__days" role="list">
        <li v-for="group in groups" :key="group.key">
          <h2 class="events__day-heading">{{ group.label }}</h2>

          <ul class="events__list" role="list">
            <li v-for="event in group.events" :key="event.eventId">
              <EventCard
                :event="event"
                :to="{ name: RouteNames.eventDetail, params: { eventId: event.eventId } }"
              />
            </li>
          </ul>
        </li>
      </ul>
    </AppAsyncState>
  </section>
</template>

<style scoped>
.events__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 24px;
}

.events__title {
  margin: 0;
}

.events__days {
  display: flex;
  flex-direction: column;
  gap: 40px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.events__day-heading {
  margin: 0 0 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--color-border);
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
