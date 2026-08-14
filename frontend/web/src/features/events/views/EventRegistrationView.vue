<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import AppAction from '@/components/AppAction.vue'
import AppField from '@/components/AppField.vue'
import { useFormFailure } from '@/composables/useFormFailure'
import { isApiError } from '@/services/http/ApiError'

import EventCard from '../components/EventCard.vue'
import { useEvent } from '../composables/useEvent'
import { useEventRegistration } from '../composables/useEventRegistration'

const UNEXPECTED_FAILURE = 'We could not register you just now. Please try again.'

const props = defineProps<{
  eventId: string
}>()

const { event, isLoading, error, load } = useEvent(() => props.eventId)

const { isRegistered, isSaving, failure, register, clearFailure } =
  useEventRegistration(() => props.eventId)

const { fieldError, formError } = useFormFailure(failure, UNEXPECTED_FAILURE)

const isNotFound = computed(
  () => isApiError(error.value) && error.value.status === 404,
)

const name = ref('')

watch(name, () => clearFailure())

function submit() {
  void register({ name: name.value })
}
</script>

<template>
  <section class="event-registration">
    <p v-if="isLoading">Loading event…</p>

    <template v-else-if="isNotFound">
      <h1 class="event-registration__title">Event not found</h1>

      <p class="event-registration__message">
        That event does not exist, or it was removed.
      </p>
    </template>

    <template v-else-if="error">
      <p>We could not load this event just now. Please try again.</p>

      <AppAction class="event-registration__retry" @click="load">Try again</AppAction>
    </template>

    <template v-else-if="event">
      <h1 class="event-registration__title">
        {{ isRegistered ? 'You are registered' : 'Register' }}
      </h1>

      <EventCard class="event-registration__event" :event="event" />

      <p v-if="isRegistered" class="event-registration__message">
        We have your name down for this event. Show this screen at the table.
      </p>

      <form v-else class="event-registration__form" @submit.prevent="submit">
        <p v-if="formError" class="event-registration__error" role="alert">
          {{ formError }}
        </p>

        <AppField v-slot="{ id, describedBy, invalid }" label="Name" :error="fieldError('Name')">
          <input
            :id="id"
            v-model="name"
            autocomplete="name"
            :aria-describedby="describedBy"
            :aria-invalid="invalid"
            required
          />
        </AppField>

        <AppAction type="submit" primary :disabled="isSaving">
          {{ isSaving ? 'Registering…' : 'Register' }}
        </AppAction>
      </form>
    </template>
  </section>
</template>

<style scoped>
.event-registration__title {
  margin-top: 0;
}

.event-registration__message {
  margin-top: 16px;
  line-height: 1.6;
  color: var(--color-text-strong);
}

.event-registration__retry {
  margin-top: 16px;
}

.event-registration__event {
  margin-top: 24px;
  max-width: 480px;
}

.event-registration__form {
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 480px;
  margin-top: 32px;
}

.event-registration__error {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--color-danger-border);
  border-radius: 6px;
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
</style>
