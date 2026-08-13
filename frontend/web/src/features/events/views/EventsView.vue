<script setup lang="ts">
// Events landing page: lists every scheduled event with its request states.
import EventCard from '../components/EventCard.vue'
import { useEvents } from '../composables/useEvents'

const { events, isLoading, error, refresh } = useEvents()

refresh()
</script>

<template>
  <section>
    <h1 class="events__title">Events</h1>

    <p v-if="isLoading">Loading events…</p>

    <p v-else-if="error">We could not load events just now. Please try again.</p>

    <p v-else-if="events.length === 0">No events are scheduled yet.</p>

    <ul v-else class="events__list">
      <li v-for="event in events" :key="event.id">
        <EventCard :event="event" />
      </li>
    </ul>
  </section>
</template>

<style scoped>
.events__title {
  margin-top: 0;
}

.events__list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin: 0;
  padding: 0;
  list-style: none;
}
</style>
