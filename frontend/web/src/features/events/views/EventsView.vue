<script setup lang="ts">
// Events page: lists every known event. Data is local until the API exists.
import EventCard from '../components/EventCard.vue'
import { events } from '../api/eventsApi'
</script>

<template>
  <section>
    <h1 class="events__title">Events</h1>

    <ul class="events__list">
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
