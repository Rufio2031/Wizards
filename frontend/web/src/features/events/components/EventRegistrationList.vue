<script setup lang="ts">
// The players registered for an event, in the order they registered.
import { useEventRegistrations } from '../composables/useEventRegistrations'

const props = defineProps<{
  eventId: string
  registrationLimit: number
}>()

const { registrations, isLoading, error } = useEventRegistrations(() => props.eventId)
</script>

<template>
  <div class="event-registration-list">
    <h3 class="event-registration-list__heading">
      Registered players

      <span class="event-registration-list__count">
        {{ registrations.length }} of {{ registrationLimit }}
      </span>
    </h3>

    <p v-if="isLoading">Loading registrations…</p>

    <p v-else-if="error">
      We could not load the registrations just now. Please try again.
    </p>

    <p v-else-if="registrations.length === 0">Nobody has registered yet.</p>

    <!-- Registrations only ever append, so a position is a stable enough key. -->
    <ol v-else class="event-registration-list__names">
      <li v-for="(registration, position) in registrations" :key="position">
        {{ registration.name }}
      </li>
    </ol>
  </div>
</template>

<style scoped>
.event-registration-list__heading {
  display: flex;
  align-items: baseline;
  gap: 8px;
  margin: 0 0 12px;
  font-size: 1.125rem;
}

.event-registration-list__count {
  font-family: var(--font-sans);
  font-size: 0.875rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-status-muted);
}

.event-registration-list__names {
  margin: 0;
  padding-left: 20px;
  line-height: 1.8;
  color: var(--color-text-strong);
}
</style>
