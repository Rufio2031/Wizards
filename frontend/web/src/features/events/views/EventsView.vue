<script setup lang="ts">
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

      <button class="events__retry" type="button" @click="load">
        Try again
      </button>
    </template>

    <p v-else-if="events.length === 0">No events are scheduled yet.</p>

    <ul v-else class="events__list">
      <li v-for="event in events" :key="event.eventId">
        <EventCard :event="event" />
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
  padding: 8px 16px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  font: inherit;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  cursor: pointer;
}

.events__retry:hover {
  box-shadow: var(--shadow-sm);
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
